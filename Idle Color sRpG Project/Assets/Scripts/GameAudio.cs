using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// BGM と SE。
// 通常BGMは healing08、ストーリー閲覧中は piano18。
// 通常クリックは system48、生産スピードアップは system46。
// RGB生産タップとスピードアップボタンには通常SEを付けない。
// スライダーは指を離したときに通常SE。
// 音量は 0〜10（0=消音）の11段階。
public sealed class GameAudio : MonoBehaviour
{
    public const int VolumeMax = 10;
    public const int DefaultBgmVolume = 5;
    public const int DefaultSeVolume = 3;
    // SE は最大でも実音量の 80% まで。
    const float SeVolumeScale = 0.8f;
    // スライダー段階 0〜10 → 実音量(0〜1)。低域を抑えて湾曲させる。
    static readonly float[] VolumeCurve =
    {
        0.00f, // 0
        0.03f, // 1
        0.06f, // 2
        0.18f, // 3
        0.30f, // 4
        0.42f, // 5
        0.54f, // 6
        0.66f, // 7
        0.78f, // 8
        0.90f, // 9
        1.00f, // 10
    };
    const string FieldBgmPath = "Audio/BGM/maou_bgm_healing08";
    const string StoryBgmPath = "Audio/BGM/maou_bgm_piano18";
    const string UiSePath = "Audio/SE/maou_se_system48";
    const string SpeedSePath = "Audio/SE/maou_se_system46";

    static GameAudio _instance;
    static readonly HashSet<int> BoundButtons = new HashSet<int>();
    static readonly HashSet<int> BoundSliders = new HashSet<int>();

    AudioSource _bgm;
    AudioSource _se;
    AudioClip _fieldBgmClip;
    AudioClip _storyBgmClip;
    AudioClip _uiSe;
    AudioClip _speedSe;
    int _bgmVolume = DefaultBgmVolume;
    int _seVolume = DefaultSeVolume;
    bool _storyBgm;
    bool _ready;

    public static int BgmVolume
    {
        get { return Ensure()._bgmVolume; }
    }

    public static int SeVolume
    {
        get { return Ensure()._seVolume; }
    }

    public static GameAudio Ensure()
    {
        if (_instance != null)
            return _instance;
        GameObject obj = new GameObject("GameAudio");
        DontDestroyOnLoad(obj);
        _instance = obj.AddComponent<GameAudio>();
        _instance.Boot();
        return _instance;
    }

    void Boot()
    {
        if (_ready)
            return;
        _fieldBgmClip = Resources.Load<AudioClip>(FieldBgmPath);
        _storyBgmClip = Resources.Load<AudioClip>(StoryBgmPath);
        _uiSe = Resources.Load<AudioClip>(UiSePath);
        _speedSe = Resources.Load<AudioClip>(SpeedSePath);
        if (_fieldBgmClip == null)
            Debug.LogWarning("通常BGM が読めませんでした: Resources/" + FieldBgmPath);
        if (_storyBgmClip == null)
            Debug.LogWarning("ストーリーBGM が読めませんでした: Resources/" + StoryBgmPath);
        if (_uiSe == null)
            Debug.LogWarning("通常SE が読めませんでした: Resources/" + UiSePath);
        if (_speedSe == null)
            Debug.LogWarning("スピードアップSE が読めませんでした: Resources/" + SpeedSePath);

        _bgm = gameObject.AddComponent<AudioSource>();
        _bgm.playOnAwake = false;
        _bgm.loop = true;
        _bgm.spatialBlend = 0f;
        _bgm.clip = _fieldBgmClip;

        _se = gameObject.AddComponent<AudioSource>();
        _se.playOnAwake = false;
        _se.loop = false;
        _se.spatialBlend = 0f;

        ApplyVolumes();
        if (_fieldBgmClip != null && _bgmVolume > 0 && !_bgm.isPlaying)
            _bgm.Play();
        _ready = true;
    }

    // ストーリー閲覧中は piano18、閉じたら通常BGMへ戻す。
    public static void EnterStoryBgm()
    {
        Ensure().SwitchBgm(true);
    }

    public static void LeaveStoryBgm()
    {
        Ensure().SwitchBgm(false);
    }

    void SwitchBgm(bool story)
    {
        AudioClip clip = story ? _storyBgmClip : _fieldBgmClip;
        if (clip == null)
            clip = _fieldBgmClip;
        _storyBgm = story && clip == _storyBgmClip;
        if (_bgm == null)
            return;
        if (_bgm.clip == clip)
        {
            ApplyVolumes();
            if (clip != null && _bgmVolume > 0 && !_bgm.isPlaying)
                _bgm.Play();
            return;
        }
        _bgm.clip = clip;
        ApplyVolumes();
        if (clip == null || _bgmVolume <= 0)
        {
            _bgm.Stop();
            return;
        }
        _bgm.Play();
    }

    public static void PlayUi()
    {
        Ensure().PlayClip(Ensure()._uiSe);
    }

    public static void PlaySpeedUp()
    {
        Ensure().PlayClip(Ensure()._speedSe);
    }

    public static void BindUi(Button button)
    {
        if (button == null || SkipsUiSe(button))
            return;
        int id = button.GetInstanceID();
        if (!BoundButtons.Add(id))
            return;
        button.onClick.AddListener(() => PlayUiIfEnabled(button));
    }

    static void PlayUiIfEnabled(Button button)
    {
        if (button == null || !button.IsActive() || !button.interactable)
            return;
        PlayUi();
    }

    // スライダーはドラッグ中ではなく、離したときだけ通常SE。
    public static void BindSlider(Slider slider)
    {
        if (slider == null)
            return;
        int id = slider.GetInstanceID();
        if (!BoundSliders.Add(id))
            return;

        EventTrigger trigger = slider.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = slider.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerUp;
        entry.callback.AddListener(_ => PlayUiIfSliderEnabled(slider));
        trigger.triggers.Add(entry);
    }

    static void PlayUiIfSliderEnabled(Slider slider)
    {
        if (slider == null || !slider.IsActive() || !slider.interactable)
            return;
        PlayUi();
    }

    // 画面上のボタンへ通常SEを付ける。RGB生産とスピードアップは除く。
    public static void BindAllButtons()
    {
        Ensure();
        Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null || !button.gameObject.scene.IsValid())
                continue;
            BindUi(button);
        }
        Slider[] sliders = Resources.FindObjectsOfTypeAll<Slider>();
        for (int i = 0; i < sliders.Length; i++)
        {
            Slider slider = sliders[i];
            if (slider == null || !slider.gameObject.scene.IsValid())
                continue;
            BindSlider(slider);
        }
    }

    public static void BindAllButtons(Transform root)
    {
        if (root == null)
            return;
        Ensure();
        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
            BindUi(buttons[i]);
        Slider[] sliders = root.GetComponentsInChildren<Slider>(true);
        for (int i = 0; i < sliders.Length; i++)
            BindSlider(sliders[i]);
    }

    static bool SkipsUiSe(Button button)
    {
        if (button == null)
            return true;
        string name = button.gameObject.name;
        if (string.IsNullOrEmpty(name))
            return false;
        if (name == "ButtonR" || name == "ButtonG" || name == "ButtonB")
            return true;
        if (name.Contains("SpeedUp"))
            return true;
        return false;
    }

    public static void SetBgmVolume(int step)
    {
        GameAudio audio = Ensure();
        audio._bgmVolume = ClampStep(step);
        audio.ApplyVolumes();
        if (audio._bgm == null || audio._bgm.clip == null)
            return;
        if (audio._bgmVolume <= 0)
        {
            if (audio._bgm.isPlaying)
                audio._bgm.Pause();
            return;
        }
        if (!audio._bgm.isPlaying)
            audio._bgm.Play();
    }

    public static void SetSeVolume(int step)
    {
        GameAudio audio = Ensure();
        audio._seVolume = ClampStep(step);
        audio.ApplyVolumes();
    }

    public static void WriteSave(StreamWriter sw)
    {
        if (sw == null)
            return;
        GameAudio audio = Ensure();
        sw.WriteLine("BgmVolume," + audio._bgmVolume.ToString(CultureInfo.InvariantCulture));
        sw.WriteLine("SeVolume," + audio._seVolume.ToString(CultureInfo.InvariantCulture));
    }

    public static bool TryReadSave(string[] values)
    {
        if (values == null || values.Length < 2 || string.IsNullOrEmpty(values[0]))
            return false;
        string key = values[0].Trim();
        int step;
        if (!int.TryParse(values[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out step))
            return key == "BgmVolume" || key == "SeVolume";
        if (key == "BgmVolume")
        {
            SetBgmVolume(step);
            return true;
        }
        if (key == "SeVolume")
        {
            SetSeVolume(step);
            return true;
        }
        return false;
    }

    public static void ResetToDefault()
    {
        SetBgmVolume(DefaultBgmVolume);
        SetSeVolume(DefaultSeVolume);
    }

    void PlayClip(AudioClip clip)
    {
        if (clip == null || _se == null || _seVolume <= 0)
            return;
        _se.PlayOneShot(clip, StepToSeVolume(_seVolume));
    }

    void ApplyVolumes()
    {
        if (_bgm != null)
            _bgm.volume = StepToVolume(_bgmVolume);
        if (_se != null)
            _se.volume = 1f;
    }

    static int ClampStep(int step)
    {
        if (step < 0)
            return 0;
        if (step > VolumeMax)
            return VolumeMax;
        return step;
    }

    static float StepToVolume(int step)
    {
        step = ClampStep(step);
        if (step < 0 || step >= VolumeCurve.Length)
            return 0f;
        return VolumeCurve[step];
    }

    static float StepToSeVolume(int step)
    {
        return StepToVolume(step) * SeVolumeScale;
    }
}
