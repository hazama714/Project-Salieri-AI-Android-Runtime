using System;

[Serializable]
public class VoiceModelInfo
{
    public string fileName;
    public string filePath;
    public VoiceSpeakerInfo[] speakers;

    public VoiceModelInfo(string fileName, string filePath, VoiceSpeakerInfo[] speakers)
    {
        this.fileName = fileName;
        this.filePath = filePath;
        this.speakers = speakers;
    }
}