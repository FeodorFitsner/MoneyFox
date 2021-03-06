using Android.Content;
using Android.Widget;
using Autofac;
using Autofac.Extras.MvvmCross;
using Cheesebaron.MvxPlugins.Connectivity;
using Clans.Fab;
using MoneyFox.Business;
using MoneyFox.Droid.CustomBinding;
using MvvmCross.Core.ViewModels;
using MvvmCross.Droid.Platform;
using MvvmCross.Droid.Views;
using MvvmCross.Platform;
using MvvmCross.Platform.Platform;
using MvvmCross.Platform.Plugins;
using MoneyFox.Foundation.Resources;
using MvvmCross.Binding.Bindings.Target.Construction;
using MvvmCross.Localization;
using MvvmCross.Platform.Converters;
using MvvmCross.Platform.IoC;
using PluginLoader = MvvmCross.Plugins.Email.PluginLoader;

namespace MoneyFox.Droid
{
    public class Setup : MvxAndroidSetup
    {
        public Setup(Context applicationContext)
            : base(applicationContext)
        {
        }

        protected override void FillTargetFactories(IMvxTargetBindingFactoryRegistry registry)
        {
            base.FillTargetFactories(registry);
            registry.RegisterFactory(new MvxCustomBindingFactory<LinearLayout>("WarningBackgroundShape", (view) => new WarningBackgroundShapeBinding(view)));
            registry.RegisterFactory(new MvxCustomBindingFactory<FloatingActionButton>("Click", (view) => new FloatingActionButtonClickBinding(view)));
        }

        public override void LoadPlugins(IMvxPluginManager pluginManager)
        {
            base.LoadPlugins(pluginManager);
            pluginManager.EnsurePluginLoaded<PluginLoader>();
            Mvx.RegisterSingleton<IConnectivity>(() => new Connectivity());
        }

        protected override IMvxIoCProvider CreateIocProvider()
        {
            var cb = new ContainerBuilder();

            cb.RegisterModule<BusinessModule>();
            cb.RegisterModule<DroidModule>();

            return new AutofacMvxIocProvider(cb.Build());
        }

        protected override void FillValueConverters(IMvxValueConverterRegistry registry)
        {
            base.FillValueConverters(registry);
            registry.AddOrOverwrite("Language", new MvxLanguageConverter());
        }

        protected override IMvxAndroidViewPresenter CreateViewPresenter()
        {
            var mvxFragmentsPresenter = new CustomPresenter(AndroidViewAssemblies);
            Mvx.RegisterSingleton<IMvxAndroidViewPresenter>(mvxFragmentsPresenter);
            return mvxFragmentsPresenter;
        }

        protected override IMvxApplication CreateApp()
        {
            Strings.Culture = new Localize().GetCurrentCultureInfo();
            return new App();
        }

        protected override IMvxTrace CreateDebugTrace() => new DebugTrace();
    }
}