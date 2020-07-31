namespace UnityEngine.Rendering.HighDefinition
{
    public static class HDCameraExtensions
    {
        public static T GetOrCreateExtension<T>(this Camera camera)
            where  T: Camera.Extension
        {
            if (!camera.HasExtension<T>())
                camera.CreateExtension<T>();
            return camera.GetExtension<T>();
        }
    }
}
