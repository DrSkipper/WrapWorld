using UnityEngine;

public class GeometryWrapController : MonoBehaviour
{
    public Transform GeometryParent;
    public Transform[] MoreLevelParents;
    public Transform EntranceTransform;
    public Transform ExitTransform;
    public Transform EntranceNode; // i.e. the camera for rendering wraps past the physical repetitions
    public Transform ExitNode; // i.e. the plane with render texture for rendering wraps past the physical repetitions
    public int Repetitions = 1;

    void Start()
    {
        // Position wrap copies of geometry
        //TODO: Account for differences in rotation in Entrance and Exit transforms
        Vector3 origin = this.GeometryParent.transform.position;
        Vector3 diff = this.ExitTransform.position - this.EntranceTransform.position;
        Vector3 diffCounter = Vector2.zero;

        for (int i = 0; i < this.Repetitions; ++i)
        {
            diffCounter += diff;
            Transform exitWrapCopy = Instantiate<Transform>(this.GeometryParent);
            Transform entranceWrapCopy = Instantiate<Transform>(this.GeometryParent);
            exitWrapCopy.transform.position = origin + diffCounter;
            entranceWrapCopy.transform.position = origin - diffCounter;

            if (this.MoreLevelParents != null)
            {
                for (int j = 0; j < this.MoreLevelParents.Length; ++j)
                {
                    exitWrapCopy = Instantiate<Transform>(this.MoreLevelParents[j]);
                    entranceWrapCopy = Instantiate<Transform>(this.MoreLevelParents[j]);
                    exitWrapCopy.transform.position = origin + diffCounter;
                    entranceWrapCopy.transform.position = origin - diffCounter;
                }
            }
        }

        // Position the camera and render texture plane for rendering the wrap past the physical repetitions
        //diffCounter += diff / 2;
        if (this.ExitNode != null)
            this.ExitNode.position = this.ExitNode.position + diffCounter;
        if (this.EntranceNode != null)
            this.EntranceNode.position = this.EntranceNode.position - diffCounter;
    }
}
