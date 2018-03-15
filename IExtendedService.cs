namespace KpdApps.CommonCore
{
	public interface IExtendedService
	{
		ServiceProvider Provider
		{
			get;
		}

		void Init(ServiceProvider provider);

		void InitDependencies();
	}
}
