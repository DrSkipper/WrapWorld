using UnityEngine;

public class WrapRepositioner : MonoBehaviour
{
    public Transform[] ObjectsToTrack;
    public Transform UpperBounds;
    public Transform LowerBounds;
    //TODO: Left/Right bounds, Front/Back bounds

    void Update()
    {
        float yDiff = this.UpperBounds.position.y - this.LowerBounds.position.y;
        for (int i = 0; i < this.ObjectsToTrack.Length; ++i)
        {
            Transform obj = this.ObjectsToTrack[i];
            if (obj.position.y < this.LowerBounds.position.y)
            {
                //TODO: Handle diff in other dimensions for entrance/exit
                obj.SetY(obj.position.y + yDiff);
            }
        }
    }
}
