using BlueFire.Toolkit.Sample.WinUI3.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.Sample.WinUI3.ViewModels
{
    public class ViewModelLocator : IServiceProvider
    {
        private static DispatcherQueue? dispatcherQueue;

        public static DispatcherQueue DispatcherQueue => dispatcherQueue!;

        public static ViewModelLocator Instance => (ViewModelLocator)Application.Current.Resources["Locator"];

        private IServiceProvider serviceProvider;

        public ViewModelLocator()
        {
            if (dispatcherQueue == null)
            {
                dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            }
            serviceProvider = BuildServiceProvider();
        }

        public MainWindowViewModel MainWindowViewModel => this.GetRequiredService<MainWindowViewModel>();

        private IServiceProvider BuildServiceProvider()
        {
            var services = new ServiceCollection();

            services.AddScoped(s => dispatcherQueue);
            services.AddScoped<NavigationService>();
            services.AddScoped<NavigationViewAdapter>();
            services.AddScoped<MainWindowViewModel>();

            return services.BuildServiceProvider();
        }

        public object GetService(Type serviceType) => serviceProvider.GetService(serviceType);
    }
}
