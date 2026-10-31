using System.Globalization;
using System.Runtime.Serialization;

namespace CapriDesktop;

[DataContract]
internal sealed class Preferences
{
	[DataMember]
	public int X;

	[DataMember]
	public int Y;

	[DataMember]
	public bool HasPosition;

	[DataMember]
	public int Size = 240;

	public bool Motion = true;

	[DataMember]
	public bool OnTop = true;

	[DataMember]
	public bool EyePopups = true;

	[DataMember]
	public int EyePace = 1;

	[DataMember]
	public int EyeSize = 1;

	[DataMember]
	public int EyeMost = 2;

	[DataMember]
	public long UpdateCheckedAt;

	[DataMember]
	public string Model;

	[DataMember]
	public bool Welcomed;

	[DataMember]
	public bool Met;

	[DataMember]
	public bool NoSitting;

	[DataMember]
	public bool NvidiaGlm;

	[DataMember]
	public string VoiceKey;

	[DataMember]
	public string VoiceId;

	[DataMember]
	public string VoiceModel;

	[DataMember]
	public string AgentId;

	[DataMember]
	public int AgentVersion;

	[DataMember]
	public string Mic;

	[DataMember]
	public string TextKey;

	[DataMember]
	public string TextModel;

	[DataMember]
	public string NvidiaKey;

	[DataMember]
	public string NvidiaModel;

	[DataMember]
	public string PaidKey;

	[DataMember]
	public string PaidModel;

	[DataMember]
	public int Tier;

	[DataMember]
	public bool Tiered;

	[DataMember]
	public bool Watch;

	[DataMember]
	public int GlanceGap;

	[DataMember]
	public bool CaughtUp = true;

	[DataMember]
	public bool StayOnModel;

	[DataMember]
	public bool Quiet;

	[DataMember]
	public bool Silent;

	[DataMember]
	public bool Wander;

	[DataMember]
	public bool FreeVoice;

	[DataMember]
	public bool NoDance;

	[DataMember]
	public string Language;

	[DataMember]
	public string Nickname;

	[DataMember]
	public int Wordiness;

	[DataMember]
	public int Recall = 8;

	[DataMember]
	public bool Remembers = true;

	[DataMember]
	public int Volume = 100;

	[DataMember]
	public int Sounds = 60;

	[DataMember]
	public int SleepAfter = 120;

	[DataMember]
	public bool NoIntro;

	[DataMember]
	public bool NoDiscord;

	[DataMember]
	public int Typing;

	[DataMember]
	public int BubbleTime = 1;

	[DataMember]
	public int TalkMods = 3;

	[DataMember]
	public int TalkVk = 86;

	[DataMember]
	public int MuteMods = 3;

	[DataMember]
	public int MuteVk = 78;

	[DataMember]
	public int TypeMods = 3;

	[DataMember]
	public int TypeVk = 84;

	[DataMember]
	public int HideMods = 3;

	[DataMember]
	public int HideVk = 72;

	[DataMember]
	public int QuietMods = 3;

	[DataMember]
	public int QuietVk = 80;

	[DataMember]
	public int WanderPace = 1;

	[DataMember]
	public int PopPace = 1;

	[DataMember]
	public int PerchFps = 30;

	[DataMember]
	public int MainframeSize;

	[DataMember]
	public string AgentPolyglot;

	[DataMember]
	public string AgentPersonality;

	[DataMember]
	public bool CheckIns = true;

	[DataMember]
	public string VoiceStyle;

	[DataMember]
	public bool StartupSet;

	[DataMember]
	public bool QuickHands;

	[DataMember]
	public bool Bypass;

	[DataMember]
	public string Permissions;

	[DataMember]
	public bool HeyCapri;

	[DataMember]
	public bool HdVoice;

	[DataMember]
	public bool HdGameRest;

	[DataMember]
	public int VoicePitch;

	[DataMember]
	public int VoiceSpeed = 100;

	[DataMember]
	public int VoiceRobot;

	[DataMember]
	public int VoiceGlitch;

	[DataMember]
	public int HdRobot;

	[DataMember]
	public int HdVoiceGlitch;

	[DataMember]
	public bool VoiceLayers;

	[DataMember]
	public double LayerPitch1 = 2.0;

	[DataMember]
	public int LayerDelay1 = 25;

	[DataMember]
	public double LayerPitch2 = -2.5;

	[DataMember]
	public int LayerDelay2 = 25;

	[DataMember]
	public bool ServerVoice;

	[DataMember]
	public string ServerUrl;

	[DataMember]
	public string ServerApi;

	[DataMember]
	public string ServerModel;

	[DataMember]
	public string ServerSpeaker;

	[DataMember]
	public string ServerKey;

	[DataMember]
	public int ServerRobot;

	[DataMember]
	public int ServerGlitch;

	[DataMember]
	public string AsideUrl;

	[DataMember]
	public string AsideApi;

	[DataMember]
	public string AsideModel;

	[DataMember]
	public string AsideSpeaker;

	[DataMember]
	public string AsideKey;

	[DataMember]
	public string CloudVoice;

	[DataMember]
	public bool NoPlayer;

	[DataMember]
	public int PlayerX;

	[DataMember]
	public int PlayerY;

	[DataMember]
	public bool HasPlayerSpot;

	[DataMember]
	public bool VoiceTuned;

	internal static Preferences Fresh()
	{
		Preferences preferences = new Preferences();
		preferences.Language = Brain.SystemLanguage(CultureInfo.CurrentUICulture);
		Preferences preferences2 = preferences;
		preferences2.Tune();
		return preferences2;
	}

	internal void Tune()
	{
		Volume = 80;
		VoicePitch = 3;
		VoiceSpeed = 120;
		VoiceLayers = true;
		LayerPitch1 = 2.0;
		LayerDelay1 = 25;
		LayerPitch2 = -2.5;
		LayerDelay2 = 25;
		VoiceTuned = true;
	}

	internal void TuneOnce()
	{
		if (!VoiceTuned)
		{
			if (Volume == 100 && VoicePitch == 0 && VoiceSpeed == 100 && VoiceRobot == 0 && VoiceGlitch == 0 && HdRobot == 0 && HdVoiceGlitch == 0 && !VoiceLayers && (LayerPitch1 == 1.5 || LayerPitch1 == 2.0) && LayerDelay1 == 25 && LayerPitch2 == -2.5 && LayerDelay2 == 25)
			{
				Tune();
			}
			VoiceTuned = true;
		}
	}

	[OnDeserializing]
	private void Defaults(StreamingContext context)
	{
		EyePopups = true;
		EyePace = 1;
		EyeSize = 1;
		EyeMost = 2;
		VoiceSpeed = 100;
		HdRobot = 0;
		LayerPitch1 = 2.0;
		LayerDelay1 = 25;
		LayerPitch2 = -2.5;
		LayerDelay2 = 25;
		Size = 240;
		Motion = true;
		OnTop = true;
		Recall = 8;
		Remembers = true;
		Volume = 100;
		Sounds = 60;
		SleepAfter = 120;
		BubbleTime = 1;
		TalkMods = 3;
		TalkVk = 86;
		MuteMods = 3;
		MuteVk = 78;
		TypeMods = 3;
		TypeVk = 84;
		HideMods = 3;
		HideVk = 72;
		QuietMods = 3;
		QuietVk = 80;
		WanderPace = 1;
		PopPace = 1;
		PerchFps = 30;
		MainframeSize = 0;
		CheckIns = true;
		CaughtUp = true;
	}
}
