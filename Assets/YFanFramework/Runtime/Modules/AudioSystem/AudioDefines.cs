using System;
using UnityEngine;

namespace YFan.Modules
{
    public enum AudioLayer
    {
        Master, // 主音量
        BGM,    // 背景音乐
        Sound,  // 音效
        Voice   // 语音/对白
    }

    [Serializable]
    public class AudioSettingsData
    {
        public float MasterVolume = 1.0f; // 主音量
        public float BGMVolume = 1.0f; // 背景音乐音量
        public float SoundVolume = 1.0f; // 音效音量
        public float VoiceVolume = 1.0f; // 语音/对白音量

        public bool IsMasterMute = false; // 是否静音主音量
        public bool IsBGMMute = false; // 是否静音背景音乐
        public bool IsSoundMute = false; // 是否静音音效
        public bool IsVoiceMute = false; // 是否静音语音/对白
    }

    public struct AudioPlayParams
    {
        public string Key; // 资源 Key
        public bool Loop; // 是否循环
        public float VolumeScale; // 单次播放音量缩放 (0-1)
        public float Pitch; // 音调
        public Vector3? Position; // 3D 空间位置 (null 则为 2D)
        public bool RandomPitch; // 是否随机微调音调
        public Transform FollowTarget; // 跟随目标

        public static AudioPlayParams Default => new AudioPlayParams
        {
            Loop = false,
            VolumeScale = 1f,
            Pitch = 1f,
            Position = null,
            RandomPitch = false
        };
    }
}
