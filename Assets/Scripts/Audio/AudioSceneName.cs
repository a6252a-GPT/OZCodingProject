using System.Collections.Generic;

public static class AudioSceneName
{
    private static readonly Dictionary<SceneType, string> sceneTable =
        new Dictionary<SceneType, string>()
        {
            { SceneType.TitleScene, "TitleScene" },
            { SceneType.StageScene, "StageScene" }, // 스테이지 씬 이름 //안건준 수정 - 0628
        };

    private static readonly Dictionary<string, BGMType> bgmTable =
        new Dictionary<string, BGMType>()
        {
            { "TitleScene", BGMType.Title_BGM },
            { "StageScene", BGMType.Stage_BGM }, // StageScene 진입 시 스테이지 BGM 재생 //안건준 추가 - 0628
        };

    public static string GetSceneName(SceneType sceneType)
    {
        return sceneTable.TryGetValue(sceneType, out string sceneName)
            ? sceneName
            : string.Empty;
    }

    public static BGMType GetBGMType(string sceneName)
    {
        return bgmTable.TryGetValue(sceneName, out BGMType bgmType)
            ? bgmType
            : BGMType.None;
    }
}
