using System;

namespace ProjektUppgift
{
    public static class ValidationCheck
    {
        public static void Validate(this float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || float.IsNegativeInfinity(value))
            {
                throw new Exception("Value is NaN or infinity");
            }
        }
    }
}
