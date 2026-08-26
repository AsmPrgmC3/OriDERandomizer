using Game;
using UnityEngine;

public class RandomizerMoveCameraAction : ActionMethod {
    public override void Perform(IContext context) {
        UI.Cameras.Current.CameraTarget.SetTargetPosition(Position);
        UI.Cameras.Current.MoveCameraToTargetInstantly();
    }

    public Vector3 Position;
}
