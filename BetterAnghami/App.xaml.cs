using System.Windows;
using System.Windows.Threading;

namespace MRK
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Tracer.Initialize();

            // catch everything that would otherwise die silently or crash the app
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Tracer.Info(
                Tracer.Category.App,
                $"----- session end (code {e.ApplicationExitCode}) -----"
            );
            base.OnExit(e);
        }

        private void OnDispatcherUnhandledException(
            object sender,
            DispatcherUnhandledExceptionEventArgs e
        )
        {
            Tracer.Error(Tracer.Category.App, "Unhandled UI exception", e.Exception);

            // keep the player alive; a stray dispatcher exception is usually one broken operation,
            // and the trace already has what we need. flip this to false to let it crash instead.
            e.Handled = true;
        }

        private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Tracer.Error(
                Tracer.Category.App,
                $"Fatal exception (terminating={e.IsTerminating})",
                e.ExceptionObject as Exception
            );
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            Tracer.Error(Tracer.Category.App, "Unobserved task exception", e.Exception);
            e.SetObserved();
        }
    }
}
