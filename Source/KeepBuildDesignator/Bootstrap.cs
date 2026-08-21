using System;
using System.IO;
using System.Reflection;
using Verse;
namespace KeepBuildDesignator
{
    public sealed class Bootstrap : Mod
    {
        public Bootstrap(ModContentPack content) : base(content)
        {
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                Assembly self = typeof(Bootstrap).Assembly;
                using (Stream stream = self.GetManifestResourceStream("KeepBuildDesignator.Core.bin"))
                {
                    if (stream == null) return;
                    byte[] bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, bytes.Length);
                    Assembly.Load(bytes).GetType("KeepBuildDesignator.Core")?.GetMethod("Install", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);
                }
            });
        }
    }
}
