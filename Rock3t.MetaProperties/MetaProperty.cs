// ========================================================
#undef DEBUG
using System.Timers;
using Timer = System.Timers.Timer;

// Source: https://www.codeproject.com/Articles/386911/Csharp-Easy-Extension-Properties
namespace Rock3t.MetaProperties
{
	// =====================================================
	public interface IMetaProperty
	{
		string PropertyName { get; }
		object PropertyValue { get; set; }
		bool AutoDispose { get; set; }
	}

	// =====================================================
	public interface IMetaPropertiesHolder
	{
		IEnumerable<IMetaProperty> MetaProperties { get; }
	}

	// =====================================================
	public static class MetaPropertyExtender
	{
		public static IMetaPropertiesHolder GetMetaPropertiesHolder(object host)
		{
			return MetaPropertyWorker._GetMetaPropertiesHolder(host, false); // No creation requested, can return null
		}

		// --------------------------------------------------
#if NET20 || NET30
		public static void SetMetaProperty( object host, string propertyName, object value, bool autoDispose )
		{
			MetaPropertyWorker._SetMetaProperty( host, propertyName, value, autoDispose );
		}
		public static void SetMetaProperty( object host, string propertyName, object value )
		{
			MetaPropertyWorker._SetMetaProperty( host, propertyName, value, true ); //autodispose:true
		}
		public static object GetMetaProperty( object host, string propertyName )
		{
			return MetaPropertyWorker._GetMetaProperty( host, propertyName );
		}
		public static bool TryGetMetaProperty( object host, string propertyName, out object value )
		{
			return MetaPropertyWorker._TryGetMetaProperty( host, propertyName, out value );
		}

		public static bool HasMetaProperty( object host, string propertyName )
		{
			return MetaPropertyWorker._HasMetaProperty( host, propertyName );
		}
		public static IMetaProperty RemoveMetaProperty( object host, string propertyName )
		{
			return MetaPropertyWorker._RemoveMetaProperty( host, propertyName );
		}
		public static void ClearMetaProperties( object host )
		{
			MetaPropertyWorker._ClearMetaProperties( host );
		}
		public static List<string> ListMetaProperties( object host )
		{
			return MetaPropertyWorker._ListMetaProperties( host );
		}
#else
		public static void SetMetaProperty<T>(this object host, string propertyName, T value, bool autoDispose) where T : class
		{
			MetaPropertyWorker._SetMetaProperty(host, propertyName, value, autoDispose);
		}
		public static void SetMetaProperty<T>(this object host, string propertyName, T value) where T : class
		{
			MetaPropertyWorker._SetMetaProperty(host, propertyName, value, true); //autodispose:true
		}
		public static object GetMetaProperty<T>(this object host, string propertyName) where T : class
		{
			return MetaPropertyWorker._GetMetaProperty<T>(host, propertyName);
		}
		public static bool TryGetMetaProperty<T>(this object host, string propertyName, out T value) where T : class
		{
			return MetaPropertyWorker._TryGetMetaProperty(host, propertyName, out value);
		}

		public static bool HasMetaProperty(this object host, string propertyName)
		{
			return MetaPropertyWorker._HasMetaProperty(host, propertyName);
		}
		public static IMetaProperty RemoveMetaProperty(this object host, string propertyName)
		{
			return MetaPropertyWorker._RemoveMetaProperty(host, propertyName);
		}
		public static void ClearMetaProperties(this object host)
		{
			MetaPropertyWorker._ClearMetaProperties(host);
		}
		public static List<string> ListMetaProperties(this object host)
		{
			return MetaPropertyWorker._ListMetaProperties(host);
		}
#endif
		// --------------------------------------------------
		public const int DefaultCollectorInterval = 10000;
		public const int MinimalCollectorInterval = 100;

		public static bool TrackDisposableHosts()
		{
			return MetaPropertyWorker._TrackHolders;
		}
		public static bool TrackDisposableHosts(bool track)
		{
			bool r = MetaPropertyWorker._TrackHolders; MetaPropertyWorker._TrackHolders = track;
			return r;
		}

		public static void StartCollector(int milliseconds)
		{
			// Assuring the minimum timer interval...
			milliseconds = milliseconds >= MinimalCollectorInterval ? milliseconds : MinimalCollectorInterval;

			// Creating a timer if needed...
			if (MetaPropertyWorker._Timer == null)
			{
				MetaPropertyWorker._Timer = new Timer();
				MetaPropertyWorker._Timer.Elapsed += new ElapsedEventHandler(MetaPropertyWorker._TryCollectHolders);
			}

			// And always setting the interval and enabling the timer...
			MetaPropertyWorker._Timer.Interval = milliseconds;
			MetaPropertyWorker._Timer.Enabled = true;
		}
		public static void StartCollector()
		{
			StartCollector(DefaultCollectorInterval);
		}
		public static void StopCollector()
		{
			if (MetaPropertyWorker._Timer != null)
			{
				MetaPropertyWorker._Timer.Stop();
				MetaPropertyWorker._Timer.Dispose();
				MetaPropertyWorker._Timer = null;
			}
		}
	}
}
// ========================================================
