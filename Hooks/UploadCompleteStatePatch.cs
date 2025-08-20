using System.Linq;

namespace KeepCameraAfterDeath.Patches;

public class UploadCompleteStatePatch
{
    internal static void Init()
    {
        On.UploadCompleteState.PlayVideo += UploadCompleteState_PlayVideo;
    }

    // UploadCompleteUI.PlayVideo -> DisplayVideoEval -> Calls onPlayed.invoke -> Triggers UploadVideoStation.RPC_OnEvaluationComplete
    // UploadVideoStation.RPC_OnEvaluationComplete -> calls UploadVideoStation.m_stateMachine -> UploadCompleteState.PlayVideo
    // TLDR; This method is played on UploadStation.RPC_OnEvaluationComplete

    // The original UploadCompleteState_PlayVideo method will:
    // 1. Play the video via UploadCompleteUI.PlayVideo, and
    // 2. Award moneys and views once the delegate "PlayVideo" is completed
    private static void UploadCompleteState_PlayVideo(On.UploadCompleteState.orig_PlayVideo orig, UploadCompleteState self, CameraRecording recording, int score, int views, int money, Comment[] comments)
    {
        // all the clients need to play the video, the host send out RPCs to them to set their ClientDoNotPlayTheseSpookTubeVideoWithRewards collection up
        KeepCameraAfterDeath.Instance.ListClientDoNotPlayTheseSpookTubeVideoWithRewards();

        if (KeepCameraAfterDeath.Instance.ClientDoNotPlayTheseSpookTubeVideoWithRewards.Any(_ => _.Equals(recording.videoHandle.id)))
        {
            KeepCameraAfterDeath.Logger.LogInfo($"[{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION}] This was recovered footage - host says 'do not award views or money' for video with ID #{recording.videoHandle.id}");
            self.m_ui.PlayVideo(recording, views, comments, delegate
            {
                // let the recording play, but don't bother doing anything once the recording is complete
                // (typically we would award views and money wtihin this delegate)
            });
            return;
        }
        else
        {
            KeepCameraAfterDeath.Logger.LogInfo($"[{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION}] Award views and money for video with ID #{recording.videoHandle.id}");
            orig(self, recording, score, views, money, comments);
            return;            
        }
    }
}
