using System.Reflection;

namespace DaveTheMonitor.Achievements.Patches
{
    public sealed class PatchInfo
    {
        public MethodBase Method { get; private set; }
        public MethodBase Prefix { get; private set; }
        public MethodBase Postfix { get; private set; }
        public MethodBase Transpiler { get; private set; }

        public PatchInfo(MethodBase method, MethodBase prefix, MethodBase postfix, MethodBase transpiler)
        {
            Method = method;
            Prefix = prefix;
            Postfix = postfix;
            Transpiler = transpiler;
        }
    }
}
