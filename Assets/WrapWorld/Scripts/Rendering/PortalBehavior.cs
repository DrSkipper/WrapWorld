using UnityEngine;

/**
 * PortalBehavior:
 * Handles rotating a camera/render texture pair to match the player's viewing angle so it can look like the player is actually looking through a portal/level wrap
 * Credit: http://answers.unity3d.com/questions/992289/portal-effect-using-render-textures-how-should-i-m.html
 */
public class PortalBehavior : MonoBehaviour
{
    public PortalBehavior Partner;
    public Camera Camera;
    public Facing Direction;

    public enum Facing
    {
        up,
        down,
        right,
        left,
        forward,
        back
    }

    private Vector3 cameraRotOrigin;

    private void Awake()
    {
        cameraRotOrigin = this.Camera.transform.eulerAngles;
    }

    void Update()
    {
        RotateCamera();
    }
    
    private void RotateCamera()
    {
        Transform playerCam = Camera.main.transform;
        Transform camTrans = this.Camera.transform;
        Transform partnerTrans = this.Partner.transform;
        Vector3 cameraEuler = camTrans.eulerAngles;
        
        // Find the position of the camera
        Vector3 pos = partnerTrans.InverseTransformPoint(playerCam.position);
        //camTrans.localPosition = new Vector3(-pos.x, pos.y, -pos.z);

        // Find the x,y.z-rotations
        if (this.Direction != Facing.left && this.Direction != Facing.right)
            cameraEuler.x = findX(playerCam, partnerTrans);
        if (this.Direction != Facing.down && this.Direction != Facing.up)
            cameraEuler.y = findY(playerCam, partnerTrans);
        if (this.Direction != Facing.back && this.Direction != Facing.forward)
            cameraEuler.z = findZ(playerCam, partnerTrans);

        // Apply the rotation
        camTrans.rotation = Quaternion.Euler(cameraEuler + getRotOffset() /* - cameraRotOrigin*/);
    }

    private float findX(Transform playerCam, Transform partnerTrans)
    {
        /*
        Transform prevParent = playerCam.parent;
        playerCam.SetParent(transform);

        cameraEuler.x = playerCam.localEulerAngles.x;
        playerCam.SetParent(prevParent);
        */

        //Temporarily rotate the player cam so it's flat
        Vector3 oldPlayerRot = playerCam.eulerAngles;
        playerCam.localRotation = Quaternion.Euler(oldPlayerRot.x, 0, oldPlayerRot.z);

        //Use DiegoSLTS's method for finding the y-rot.
        float retVal = SignedAngle(-partnerTrans.forward, playerCam.forward, Vector3.right);

        //Restore the player cam
        playerCam.rotation = Quaternion.Euler(oldPlayerRot);
        return retVal;
    }

    private float findY(Transform playerCam, Transform partnerTrans)
    {
        //Temporarily rotate the player cam so it's flat
        Vector3 oldPlayerRot = playerCam.eulerAngles;
        playerCam.localRotation = Quaternion.Euler(0, oldPlayerRot.y, oldPlayerRot.z);

        //Use DiegoSLTS's method for finding the y-rot.
        float retVal = SignedAngle(-partnerTrans.forward, playerCam.forward, Vector3.up);

        //Restore the player cam
        playerCam.rotation = Quaternion.Euler(oldPlayerRot);
        return retVal;
    }

    private float findZ(Transform playerCam, Transform partnerTrans)
    {
        //Temporarily rotate the player cam so it's flat
        Vector3 oldPlayerRot = playerCam.eulerAngles;
        playerCam.localRotation = Quaternion.Euler(oldPlayerRot.x, oldPlayerRot.y, 0);

        //Use DiegoSLTS's method for finding the y-rot.
        float retVal = SignedAngle(-partnerTrans.forward, playerCam.forward, Vector3.forward);

        //Restore the player cam
        playerCam.rotation = Quaternion.Euler(oldPlayerRot);
        return retVal;
    }

    private float SignedAngle(Vector3 a, Vector3 b, Vector3 n)
    {
        // angle in [0,180]
        float angle = Vector3.Angle(a, b);
        float sign = Mathf.Sign(Vector3.Dot(n, Vector3.Cross(a, b)));

        // angle in [-179,180]
        float signed_angle = angle * sign;
        while (signed_angle < 0) signed_angle += 360;

        return signed_angle;
    }

    private Vector3 getRotOffset()
    {
        switch (this.Direction)
        {
            default:
            case Facing.up:
            case Facing.right:
            case Facing.forward:
                return new Vector3(0, 0, 0);
            case Facing.down:
                return new Vector3(0, 180, 0);
            case Facing.left:
                return new Vector3(180, 0, 0);
            case Facing.back:
                return new Vector3(0, 0, 180);
        }
    }
}
