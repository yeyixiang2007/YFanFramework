using Cysharp.Threading.Tasks;
using QFramework;

namespace YFan.Modules
{
    public interface IAudioSystem : ISystem
    {
        // --- BGM 控制 ---

        /// <summary>
        /// 播放背景音乐 (支持淡入淡出)
        /// </summary>
        UniTask PlayBGM(string key, bool fade = true, float fadeDuration = 1.0f);

        /// <summary>
        /// 停止背景音乐 (支持淡入淡出)
        /// </summary>
        void StopBGM(bool fade = true, float fadeDuration = 1.0f);

        /// <summary>
        /// 暂停/恢复背景音乐
        /// </summary>
        void PauseBGM(bool pause);

        // --- 音效控制 ---

        /// <summary>
        /// 播放音效 (Fire and Forget)
        /// </summary>
        void PlaySound(string key, float volumeScale = 1f);

        /// <summary>
        /// 播放音效 (高级参数：3D位置、随机音调等)
        /// </summary>
        void PlaySound(AudioPlayParams param);

        // --- 语音控制 ---
        /// <summary>
        /// 播放语音/对白 (Fire and Forget)
        /// </summary>
        void PlayVoice(string key, bool stopCurrent = true);

        /// <summary>
        /// 停止语音/对白
        /// </summary>
        void StopVoice();

        // --- 设置 ---

        /// <summary>
        /// 设置音量
        /// </summary>
        void SetVolume(AudioLayer layer, float volume);

        /// <summary>
        /// 获取音量
        /// </summary>
        float GetVolume(AudioLayer layer);

        /// <summary>
        /// 设置静音
        /// </summary>
        void SetMute(AudioLayer layer, bool isMute);

        /// <summary>
        /// 是否静音
        /// </summary>
        bool IsMute(AudioLayer layer);
    }
}
