using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Net;

namespace KpdApps.CommonCore
{
	public sealed class ServiceProvider : IServiceProvider
	{
		private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

		public ICredentials Credentials { get; set; }

		private ConfigurationSettings _settings;
		public ConfigurationSettings Settings
		{
			get { return _settings; }
			private set
			{
				if (_settings == value)
					return;

				_settings = value;
				if (!_settings.UseDefaultCredentials)
					Credentials = _settings.Credential;
			}
		}

		public ServiceProvider(bool initializeInternal = false)
		{
			Credentials = CredentialCache.DefaultCredentials;
			if (initializeInternal)
				return;

			if (ConfigurationSettings.Current == null)
			{
				Settings = new ConfigurationSettings();
				return;
			}

			Settings = ConfigurationSettings.Current;

			NameValueCollection services = (NameValueCollection)ConfigurationManager.GetSection("Services");
			if (services == null)
				return;

			foreach (string key in services.AllKeys)
			{
				try
				{
					Type t = Type.GetType(services[key], false);
					if (t == null)
						continue;

					IExtendedService extSvc = (IExtendedService)Activator.CreateInstance(t);
					_services.Add(t, extSvc);
					extSvc.Init(this);
				}
				catch (TypeLoadException)
				{
					throw new Exception(string.Format(System.Globalization.CultureInfo.InvariantCulture, "Cannot load type '{0}' (key: '{1}')", services[key], key));
				}
			}

			foreach (IExtendedService extSvc in _services.Values)
				extSvc.InitDependencies();
		}

		public static ServiceProvider Create(ConfigurationSettings settings)
		{
			var provider = new ServiceProvider
			{
				Credentials = CredentialCache.DefaultCredentials,
			};

			if (settings == null)
			{
				provider.Settings = new ConfigurationSettings();
				return provider;
			}

			provider.Settings = settings;

			NameValueCollection services = (NameValueCollection)ConfigurationManager.GetSection("Services");
			if (services != null)
			{
				foreach (string key in services.AllKeys)
				{
					try
					{
						Type t = Type.GetType(services[key], false);
						if (t != null)
						{
							provider.Register(t, false);
						}
					}
					catch (TypeLoadException)
					{
						throw new Exception(string.Format(System.Globalization.CultureInfo.InvariantCulture, "Cannot load type '{0}' (key: '{1}')", services[key], key));
					}
				}

				foreach (IExtendedService extSvc in provider._services.Values)
					extSvc.InitDependencies();
			}
			return provider;
		}

		public void Register(Type serviceType)
		{
			Register(serviceType, true);
		}

		private void Register(Type serviceType, bool initializeDependencies)
		{
			var obj = Activator.CreateInstance(serviceType);

			var extSvc = obj as IExtendedService;
			if (extSvc != null)
			{
				extSvc.Init(this);

				if (initializeDependencies)
					extSvc.InitDependencies();
			}
			_services.Add(serviceType, obj);
		}

		public void Register(Type serviceType, object instance)
		{
			var ies = instance as IExtendedService;
			ies?.Init(this);
			_services.Add(serviceType, instance);
		}

		public void UnRegister(Type serviceType)
		{
			if (_services.ContainsKey(serviceType))
				_services.Remove(serviceType);
		}

		#region IServiceProvider

		public object GetService(Type serviceType)
		{
			if (serviceType.IsInterface)
			{
				foreach (Type t in _services.Keys)
				{
					Type i = t.GetInterface(serviceType.FullName);
					if (i != null)
					{
						return _services[t];
					}
				}
			}

			if (_services.ContainsKey(serviceType))
			{
				return _services[serviceType];
			}

			return null;
		}

		#endregion
	}
}