using System;
using System.Threading;
using System.Web.Hosting;

namespace LibraryOnline.Services
{
    public sealed class Maintenance : IRegisteredObject
    {
        private Timer timer;
        private int busy;

        public static void Start()
        {
            var m = new Maintenance();
            HostingEnvironment.RegisterObject(m);
            m.timer = new Timer(
                _ => m.Run(),
                null,
                TimeSpan.FromSeconds(30),
                TimeSpan.FromMinutes(1)
            );
        }

        private void Run()
        {
            if (Interlocked.Exchange(ref busy, 1) != 0)
                return;
            try
            {
                using (var s = new LibraryService())
                    s.Sweep();
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.TraceError(e.ToString());
            }
            finally
            {
                Interlocked.Exchange(ref busy, 0);
            }
        }

        public void Stop(bool immediate)
        {
            timer?.Dispose();
            HostingEnvironment.UnregisterObject(this);
        }
    }
}
