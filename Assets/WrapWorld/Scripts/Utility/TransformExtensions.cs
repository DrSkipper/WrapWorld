using UnityEngine;


public static class TransformExtensions
{
    public static void SetPosition(this Transform self, float x, float y, float z)
    {
        self.position = new Vector3(x, y, z);
    }

    public static void SetPosition2D(this Transform self, float x, float y)
    {
        self.position = new Vector3(x, y, self.position.z);
    }

    public static void SetPosition2D(this Transform self, Vector2 pos)
    {
        self.SetPosition2D(pos.x, pos.y);
    }

    public static void SetLocalPosition(this Transform self, float x, float y, float z)
    {
        self.localPosition = new Vector3(x, y, z);
    }

    public static void SetLocalPosition2D(this Transform self, Vector2 pos)
    {
        self.localPosition = new Vector3(pos.x, pos.y, self.localPosition.z);
    }

    public static void SetLocalPosition2D(this Transform self, float x, float y)
    {
        self.localPosition = new Vector3(x, y, self.localPosition.z);
    }

    public static void SetX(this Transform self, float x)
    {
        self.position = new Vector3(x, self.position.y, self.position.z);
    }

    public static void SetY(this Transform self, float y)
    {
        self.position = new Vector3(self.position.x, y, self.position.z);
    }

    public static void SetZ(this Transform self, float z)
    {
        self.position = new Vector3(self.position.x, self.position.y, z);
    }

    public static void SetLocalX(this Transform self, float x)
    {
        self.localPosition = new Vector3(x, self.localPosition.y, self.localPosition.z);
    }

    public static void SetLocalY(this Transform self, float y)
    {
        self.localPosition = new Vector3(self.localPosition.x, y, self.localPosition.z);
    }

    public static void SetLocalZ(this Transform self, float z)
    {
        self.localPosition = new Vector3(self.localPosition.x, self.localPosition.y, z);
    }

    public static void SetScaleX(this Transform self, float x)
    {
        self.localScale = new Vector3(x, self.localScale.y, self.localScale.z);
    }

    public static void SetScaleY(this Transform self, float y)
    {
        self.localScale = new Vector3(self.localScale.x, y, self.localScale.z);
    }

    public static void SetScale2D(this Transform self, float x, float y)
    {
        self.localScale = new Vector3(x, y, self.localScale.z);
    }

    public static float Distance2D(this Transform self, Transform other)
    {
        return Vector2.Distance(self.position, other.position);
    }

    public static Vector2 DirectionTo2D(this Transform self, Transform other)
    {
        return ((Vector2)(other.position - self.position)).normalized;
    }

    public static void LookAt2D(this Transform self, Vector2 other, float rotOffset = 0.0f)
    {
        Vector2 diff = other - (Vector2)self.position;
        diff.Normalize();

        float rot_z = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        self.rotation = Quaternion.Euler(0.0f, 0.0f, rot_z - 90.0f + rotOffset);
    }
}
