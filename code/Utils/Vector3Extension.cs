namespace Sandbox;

public static class Vector3Extensions
{
	public static float SignedAngle( this Vector3 from, Vector3 to, Vector3 axis )
	{
		float unsignedAngle = Vector3.GetAngle( from, to );

		float cross_x = from.y * to.z - from.z * to.y;
		float cross_y = from.z * to.x - from.x * to.z;
		float cross_z = from.x * to.y - from.y * to.x;
		float sign = MathF.Sign( axis.x * cross_x + axis.y * cross_y + axis.z * cross_z );
		return unsignedAngle * sign;
	}
}
