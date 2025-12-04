using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using QFramework;
using UnityEngine;
using YFan.Attributes;
using YFan.Utils;

namespace YFan.Modules
{
    [AutoRegister(typeof(IAudioSystem))]
    public class AudioSystem : AbstractSystem, IAudioSystem
    {
        #region 内部状态

        private const string ModuleName = "AudioSystem";

        private GameObject _root; // 运行时根物体
        private Transform _soundRoot; // 音效根物体

        // BGM 双通道
        private AudioSource _bgmSourceA; // BGM 通道 A
        private AudioSource _bgmSourceB; // BGM 通道 B
        private bool _isUsingSourceA = true; // 当前是否使用通道 A
        private string _currentBgmKeyA; // 当前通道 A 播放的 BGM Key
        private string _currentBgmKeyB; // 当前通道 B 播放的 BGM Key
        private AudioSource CurrentBGMSource => _isUsingSourceA ? _bgmSourceA : _bgmSourceB; // 当前正在使用的 BGM 通道
        private string CurrentBGMKey => _isUsingSourceA ? _currentBgmKeyA : _currentBgmKeyB; // 当前正在播放的 BGM Key

        // Voice
        private AudioSource _voiceSource; // 语音通道
        private string _currentVoiceKey; // 当前播放的语音 Key

        // SFX
        private readonly Queue<AudioSource> _soundPool = new Queue<AudioSource>(); // 音效池
        private readonly List<AudioSource> _activeSounds = new List<AudioSource>(); // 当前激活的音效
        private const int MaxSoundInstances = 24; // 最大音效实例数
        private readonly Dictionary<string, float> _soundCooldowns = new Dictionary<string, float>(); // 音效冷却时间

        // 数据与状态
        private AudioSettingsData _settings; // 当前音频设置
        private bool _isDirty = false; // 是否需要保存设置
        private CancellationTokenSource _autoSaveCts; // 自动保存循环 Token

        // 依赖
        private IAssetUtil _assetUtil; // 资源加载工具

        #endregion

        #region 初始化与销毁

        protected override void OnInit()
        {
            _assetUtil = this.GetUtility<IAssetUtil>();

            // 加载设置 (接入 SaveUtil)
            LoadSettings();

            // 初始化物体
            InitObjectTree();

            // 启动自动保存循环
            _autoSaveCts = new CancellationTokenSource();
            AutoSaveLoop(_autoSaveCts.Token).RunSafe(ModuleName);

            YLog.Info("AudioSystem 初始化完成", ModuleName);
        }

        private void InitObjectTree()
        {
            _root = new GameObject(ConfigKeys.AudioSystemRuntime);
            UnityEngine.Object.DontDestroyOnLoad(_root);

            var bgmRoot = new GameObject(ConfigKeys.AudioSettingBGMRoot).transform;
            bgmRoot.SetParent(_root.transform);
            _bgmSourceA = CreateSource("BGM_A", bgmRoot, true);
            _bgmSourceB = CreateSource("BGM_B", bgmRoot, true);

            var voiceRoot = new GameObject(ConfigKeys.AudioSettingVoiceRoot).transform;
            voiceRoot.SetParent(_root.transform);
            _voiceSource = CreateSource("Voice_Source", voiceRoot, false);

            _soundRoot = new GameObject(ConfigKeys.AudioSettingSFXPoolRoot).transform;
            _soundRoot.SetParent(_root.transform);

            for (int i = 0; i < 10; i++)
            {
                var source = CreateSource($"SFX_{i}", _soundRoot, false);
                source.gameObject.SetActive(false);
                _soundPool.Enqueue(source);
            }

            // 初始化完成后应用一次音量
            ApplyAllVolume();
        }

        private AudioSource CreateSource(string name, Transform parent, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }

        public void Dispose()
        {
            TaskUtil.CancelSafe(ref _autoSaveCts);

            // 销毁前强制保存一次
            if (_isDirty) SaveSettings();

            if (_root != null) UnityEngine.Object.Destroy(_root);

            // 清理资源引用
            if (!string.IsNullOrEmpty(_currentBgmKeyA)) _assetUtil.Release(_currentBgmKeyA);
            if (!string.IsNullOrEmpty(_currentBgmKeyB)) _assetUtil.Release(_currentBgmKeyB);
            if (!string.IsNullOrEmpty(_currentVoiceKey)) _assetUtil.Release(_currentVoiceKey);
        }

        #endregion

        #region BGM Logic

        public async UniTask PlayBGM(string key, bool fade = true, float fadeDuration = 0.5f)
        {
            if (CurrentBGMSource.isPlaying && CurrentBGMKey == key) return;

            var clip = await _assetUtil.LoadAsync<AudioClip>(key);
            if (clip == null) return;
            clip.name = key;

            AudioSource oldSource = CurrentBGMSource;
            string oldKey = CurrentBGMKey;

            _isUsingSourceA = !_isUsingSourceA;
            AudioSource newSource = CurrentBGMSource;
            if (_isUsingSourceA) _currentBgmKeyA = key; else _currentBgmKeyB = key;

            newSource.clip = clip;
            newSource.volume = 0f;
            newSource.Play();

            float targetVol = GetRealVolume(AudioLayer.BGM);

            if (!fade || fadeDuration <= 0f)
            {
                newSource.volume = targetVol;
                StopSourceAndRelease(oldSource, oldKey);
            }
            else
            {
                float timer = 0f;
                await TaskUtil.WaitUntil(() =>
                {
                    timer += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(timer / fadeDuration);
                    newSource.volume = Mathf.Lerp(0f, targetVol, t);
                    if (oldSource.isPlaying)
                        oldSource.volume = Mathf.Lerp(targetVol, 0f, t);
                    return t >= 1.0f;
                }, fadeDuration + 0.1f);

                StopSourceAndRelease(oldSource, oldKey);
                newSource.volume = targetVol;
            }
        }

        public void StopBGM(bool fade = true, float fadeDuration = 0.5f)
        {
            StopBGMInternal(fade, fadeDuration).Forget();
        }

        private async UniTaskVoid StopBGMInternal(bool fade, float duration)
        {
            AudioSource source = CurrentBGMSource;
            string key = CurrentBGMKey;

            if (source == null || !source.isPlaying) return;

            if (fade && duration > 0)
            {
                float startVol = source.volume;
                float timer = 0f;
                await TaskUtil.WaitUntil(() =>
                {
                    if (source == null) return true;
                    timer += Time.unscaledDeltaTime;
                    float t = timer / duration;
                    source.volume = Mathf.Lerp(startVol, 0f, t);
                    return t >= 1f;
                }, duration + 0.1f);
            }

            StopSourceAndRelease(source, key);
            if (_isUsingSourceA) _currentBgmKeyA = null; else _currentBgmKeyB = null;
        }

        public void PauseBGM(bool pause)
        {
            if (pause) CurrentBGMSource.Pause();
            else CurrentBGMSource.UnPause();
        }

        private void StopSourceAndRelease(AudioSource source, string key)
        {
            if (source != null)
            {
                source.Stop();
                source.clip = null;
            }
            if (!string.IsNullOrEmpty(key)) _assetUtil.Release(key);
        }

        #endregion

        #region SFX Logic

        public void PlaySound(string key, float volumeScale = 1)
            => PlaySound(new AudioPlayParams { Key = key, VolumeScale = volumeScale });

        public void PlaySound(AudioPlayParams param)
        {
            if (string.IsNullOrEmpty(param.Key)) return;
            if (!CheckCooldown(param.Key)) return;

            PlaySoundTask(param).Forget();
        }

        private bool CheckCooldown(string key)
        {
            float now = Time.unscaledTime;
            if (_soundCooldowns.TryGetValue(key, out float lastTime))
            {
                if (now - lastTime < 0.05f) return false;
            }
            _soundCooldowns[key] = now;
            return true;
        }

        private async UniTask PlaySoundTask(AudioPlayParams param)
        {
            var clip = await _assetUtil.LoadAsync<AudioClip>(param.Key);
            if (clip == null) return;

            AudioSource source = GetSoundSource();
            if (source == null)
            {
                _assetUtil.Release(param.Key);
                return;
            }

            source.clip = clip;
            source.transform.position = param.Position ?? Vector3.zero;
            source.spatialBlend = param.Position.HasValue ? 1.0f : 0.0f;
            source.loop = param.Loop;
            source.pitch = param.RandomPitch ? UnityEngine.Random.Range(0.9f, 1.1f) : param.Pitch;
            source.volume = GetRealVolume(AudioLayer.Sound) * param.VolumeScale;

            source.gameObject.SetActive(true);
            source.Play();

            if (!param.Loop)
            {
                float duration = clip.length / Mathf.Abs(source.pitch);
                await TaskUtil.Delay(duration + 0.1f, ignoreTimeScale: true);
                RecycleSoundSource(source);
                _assetUtil.Release(param.Key);
            }
        }

        private AudioSource GetSoundSource()
        {
            if (_activeSounds.Count >= MaxSoundInstances) return null;

            AudioSource source;
            if (_soundPool.Count > 0) source = _soundPool.Dequeue();
            else source = CreateSource("SFX_Ext", _soundRoot, false);

            _activeSounds.Add(source);
            return source;
        }

        private void RecycleSoundSource(AudioSource source)
        {
            if (source == null) return;
            source.Stop();
            source.clip = null;
            source.gameObject.SetActive(false);
            if (_activeSounds.Contains(source)) _activeSounds.Remove(source);
            _soundPool.Enqueue(source);
        }

        #endregion

        #region Voice Logic

        public void PlayVoice(string key, bool stopCurrent = true)
        {
            PlayVoiceTask(key, stopCurrent).Forget();
        }

        public void StopVoice()
        {
            if (_voiceSource.isPlaying)
            {
                _voiceSource.Stop();
                _voiceSource.clip = null;
            }
            if (!string.IsNullOrEmpty(_currentVoiceKey))
            {
                _assetUtil.Release(_currentVoiceKey);
                _currentVoiceKey = null;
            }
        }

        private async UniTask PlayVoiceTask(string key, bool stopCurrent)
        {
            if (stopCurrent) StopVoice();
            if (!stopCurrent && _voiceSource.isPlaying) return;

            var clip = await _assetUtil.LoadAsync<AudioClip>(key);
            if (clip == null) return;

            _currentVoiceKey = key;
            _voiceSource.clip = clip;
            _voiceSource.volume = GetRealVolume(AudioLayer.Voice);
            _voiceSource.Play();

            float duration = clip.length;
            await TaskUtil.Delay(duration + 0.1f);

            if (_currentVoiceKey == key)
            {
                _voiceSource.clip = null;
                _assetUtil.Release(key);
                _currentVoiceKey = null;
            }
        }

        #endregion

        #region Volume & Persistence

        public void SetVolume(AudioLayer layer, float volume)
        {
            volume = Mathf.Clamp01(volume);
            switch (layer)
            {
                case AudioLayer.Master: _settings.MasterVolume = volume; break;
                case AudioLayer.BGM: _settings.BGMVolume = volume; break;
                case AudioLayer.Sound: _settings.SoundVolume = volume; break;
                case AudioLayer.Voice: _settings.VoiceVolume = volume; break;
            }
            _isDirty = true;
            ApplyAllVolume();
        }

        public float GetVolume(AudioLayer layer)
        {
            return layer switch
            {
                AudioLayer.Master => _settings.MasterVolume,
                AudioLayer.BGM => _settings.BGMVolume,
                AudioLayer.Sound => _settings.SoundVolume,
                AudioLayer.Voice => _settings.VoiceVolume,
                _ => 1f
            };
        }

        public void SetMute(AudioLayer layer, bool isMute)
        {
            switch (layer)
            {
                case AudioLayer.Master: _settings.IsMasterMute = isMute; break;
                case AudioLayer.BGM: _settings.IsBGMMute = isMute; break;
                case AudioLayer.Sound: _settings.IsSoundMute = isMute; break;
                case AudioLayer.Voice: _settings.IsVoiceMute = isMute; break;
            }
            _isDirty = true;
            ApplyAllVolume();
        }

        public bool IsMute(AudioLayer layer)
        {
            return layer switch
            {
                AudioLayer.Master => _settings.IsMasterMute,
                AudioLayer.BGM => _settings.IsBGMMute,
                AudioLayer.Sound => _settings.IsSoundMute,
                AudioLayer.Voice => _settings.IsVoiceMute,
                _ => false
            };
        }

        private float GetRealVolume(AudioLayer layer)
        {
            if (_settings.IsMasterMute) return 0f;
            float master = _settings.MasterVolume;

            return layer switch
            {
                AudioLayer.Master => master,
                AudioLayer.BGM => _settings.IsBGMMute ? 0f : _settings.BGMVolume * master,
                AudioLayer.Sound => _settings.IsSoundMute ? 0f : _settings.SoundVolume * master,
                AudioLayer.Voice => _settings.IsVoiceMute ? 0f : _settings.VoiceVolume * master,
                _ => 0f
            };
        }

        private void ApplyAllVolume()
        {
            if (CurrentBGMSource != null) CurrentBGMSource.volume = GetRealVolume(AudioLayer.BGM);
            if (_voiceSource != null) _voiceSource.volume = GetRealVolume(AudioLayer.Voice);

            float soundVol = GetRealVolume(AudioLayer.Sound);
            foreach (var s in _activeSounds)
            {
                if (s != null) s.volume = soundVol;
            }
        }

        /// <summary>
        /// 从 SaveUtil 加载设置
        /// </summary>
        private void LoadSettings()
        {
            // 直接调用静态工具加载
            _settings = SaveUtil.Load<AudioSettingsData>(ConfigKeys.AudioSettingSaveSlot);

            // 如果不存在（第一次运行），创建默认配置
            if (_settings == null)
            {
                _settings = new AudioSettingsData();
                _isDirty = true; // 标记需要保存
            }
        }

        /// <summary>
        /// 使用 SaveUtil 保存设置
        /// </summary>
        private void SaveSettings()
        {
            // 保存，附带一个备注
            SaveUtil.Save(ConfigKeys.AudioSettingSaveSlot, _settings, ConfigKeys.AudioSettingSaveNote);
            // YLog.Info("音频设置已保存", ModuleName);
        }

        /// <summary>
        /// 自动保存循环 (每2秒检查一次)
        /// </summary>
        private async UniTask AutoSaveLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(2), ignoreTimeScale: true, cancellationToken: token);
                if (_isDirty)
                {
                    SaveSettings();
                    _isDirty = false;
                }
            }
        }

        #endregion
    }
}
