// ========================================================
#undef DEBUG
using System.ComponentModel;
using System.Diagnostics;
using System.Timers;
using Timer = System.Timers.Timer;

// Source: https://www.codeproject.com/Articles/386911/Csharp-Easy-Extension-Properties
namespace Rock3t.MetaProperties
{
    // =====================================================
	internal class MetaProperty : IMetaProperty
	{
		public string PropertyName { get; internal set; }
		public object PropertyValue { get; set; }
		public bool AutoDispose { get; set; }

		public override string ToString()
		{
			return string.Format( "{0}={1}",
				PropertyName ?? "<null>",
				PropertyValue == null ? "<null>" : PropertyValue.ToString() );
		}

		internal MetaProperty( string name, object value, bool autoDispose )
		{
			Debug.Indent();
			Debug.WriteLine( string.Format( "=MetaProperty::( {0}={1}, autodispose:{2} )", name ?? "<error>", value == null ? "<null>" : value.ToString(), autoDispose ) );
			try {
				if( name == null ) throw new ArgumentNullException( "name", "Meta Property name cannot be null." );
				if( string.IsNullOrEmpty( name ) ) throw new ArgumentException( "Meta Property name cannot be empty." );

				PropertyName = name;
				PropertyValue = value;
				AutoDispose = autoDispose;
			}
			finally { Debug.Unindent(); }
		}

		protected virtual void Dispose( bool disposing )
		{
			Debug.Indent();
			Debug.WriteLine( string.Format( "=MetaProperty[ {0} ]::Dispose( disposing:{1} )", this.ToString(), disposing ) );
			try {
				if( PropertyValue != null && PropertyValue is IDisposable && AutoDispose )
					( (IDisposable)PropertyValue ).Dispose();

				PropertyValue = null;
				PropertyName = null;
			}
			finally { Debug.Unindent(); }
		}
		public void Dispose()
		{
			Debug.Indent();
			Debug.WriteLine( string.Format( "=MetaProperty[ {0} ]::Dispose()", this.ToString() ) );
			try {
				Dispose( true );
				GC.SuppressFinalize( this );
			}
			finally { Debug.Unindent(); }
		}
		~MetaProperty()
		{
			Debug.Indent();
			Debug.WriteLine( string.Format( "=MetaProperty[ {0} ]::~()", this.ToString() ) );
			try {
				Dispose( false );
			}
			finally { Debug.Unindent(); }
		}
	}

	// =====================================================
	class MetaPropertiesHolder : Attribute, IMetaPropertiesHolder
	{
		internal Dictionary<string, MetaProperty> _MetaProperties = new Dictionary<string, MetaProperty>();
		internal WeakReference _HostReference = null;

		public override string ToString()
		{
			return ToString( false );
		}
		public string ToString( bool extended )
		{
			string s = null;

			if( _HostReference == null ) s = "<WeakReference:null>";
			else s = string.Format( "<Host:{0}, Alive:{1}>",
				_HostReference.Target == null ? "null" : _HostReference.Target.ToString(),
				_HostReference.IsAlive );

			if( extended && _MetaProperties != null ) {
				s += "{"; bool first = true; foreach( var meta in _MetaProperties ) {
					if( !first ) s += ", "; else first = false;
					s += string.Format( "{0}:{1}", meta.Value.PropertyName, meta.Value.PropertyValue == null ? "<null>" : meta.Value.PropertyValue.ToString() );
				}
				s += "}";
			}
			return s;
		}

		internal MetaPropertiesHolder( object host )
		{
			Debug.Indent();
			Debug.WriteLine( string.Format( "=MetaPropertiesHolder::( host:{0} )", host == null ? "<null>" : host.ToString() ) );
			try {
				if( host == null ) throw new ArgumentNullException( "host", "Host to extend cannot be null." );
				_HostReference = new WeakReference( host );
			}
			finally { Debug.Unindent(); }
		}	

		protected virtual void Dispose( bool disposing )
		{
			Debug.Indent();
			Debug.WriteLine( string.Format( "=MetaPropertiesHolder[ {0} ]::Dispose( disposing:{1} )", this.ToString(), disposing ) );
			try {
				if( _MetaProperties != null ) { Clear(); _MetaProperties = null; }
				_HostReference = null;
			}
			finally { Debug.Unindent(); }
		}
		public void Dispose()
		{
			Debug.Indent();
			Debug.WriteLine( string.Format( "=MetaPropertiesHolder[ {0} ]::Dispose()", this.ToString() ) );
			try {
				Dispose( true );
				GC.SuppressFinalize( this );
			}
			finally { Debug.Unindent(); }
		}
		~MetaPropertiesHolder()
		{
			Debug.Indent();
			Debug.WriteLine( string.Format( "=MetaPropertiesHolder[ {0} ]::~()", this.ToString() ) );
			try {
				Dispose( false );
			}
			finally { Debug.Unindent(); }
		}

		public void Clear()
		{
			if( _MetaProperties != null ) {
				foreach( var kvp in _MetaProperties ) kvp.Value.Dispose();
				_MetaProperties.Clear();
			}
		}

		public IEnumerable<IMetaProperty> MetaProperties
		{
			get { foreach( var kvp in _MetaProperties ) yield return kvp.Value; }
		}
	}

	// =====================================================
	internal static class MetaPropertyWorker
	{
		internal static List<WeakReference> _WeakHolders = new List<WeakReference>();
		internal static bool _TrackHolders = true;
		internal static Timer _Timer = null;

		internal static void _TryCollectHolders( object source, ElapsedEventArgs e )
		{
			Debug.Indent();
			Debug.Write( string.Format( "\n=::_TryCollectHolders(), Count:{0}", _WeakHolders.Count ) );
			try {
				bool enabled = _Timer.Enabled; _Timer.Enabled = false;
				lock( _WeakHolders ) {

					List<WeakReference> list = new List<WeakReference>();
					foreach( var weak in _WeakHolders ) {
						if( weak.Target == null ) list.Add( weak );
						else if( !weak.IsAlive ) list.Add( weak );
						else if( ( (MetaPropertiesHolder)weak.Target )._HostReference == null ) list.Add( weak );
						else if( ( (MetaPropertiesHolder)weak.Target )._HostReference.Target == null ) list.Add( weak );
						else if( ( (MetaPropertiesHolder)weak.Target )._HostReference.IsAlive != true ) list.Add( weak );
					}
					Debug.WriteLine( string.Format( ", To remove:{0}", list.Count ) );

					foreach( var weak in list ) {
						_WeakHolders.Remove( weak );
						if( weak.Target != null ) ( (MetaPropertiesHolder)weak.Target ).Dispose();
					}

					list.Clear();
					list = null;
				}
				_Timer.Enabled = enabled;
			}
			finally { Debug.Unindent(); }
			if( _Timer != null ) GC.KeepAlive( _Timer );
		}

		internal static MetaPropertiesHolder _GetMetaPropertiesHolder( object host, bool create )
		{
			MetaPropertiesHolder holder = null;
			lock( _WeakHolders ) {

				// Trying to find the holder carried in the form of an attribute by this host...
				AttributeCollection list = TypeDescriptor.GetAttributes( host );
				if( list != null ) {
					foreach( var item in list ) {
						if( item is MetaPropertiesHolder ) { holder = (MetaPropertiesHolder)item; break; }
					}
				}

				// If not found but creation requested...
				if( holder == null && create ) {
					holder = new MetaPropertiesHolder( host );
					var descriptor = TypeDescriptor.AddAttributes( host, holder );

					// Adding the descriptor as the first meta-property...
					var meta = new MetaProperty( "TypeDescriptionProvider", descriptor, true );
					holder._MetaProperties.Add( meta.PropertyName, meta );

					// And tracking the holder (as a weak reference to not interfere with GC)...
					if( _TrackHolders ) _WeakHolders.Add( new WeakReference( holder ) );
				}
			}
			// Returning found, created, or null...
			return holder;
		}

		internal static void _SetMetaProperty( object host, string propertyName, object value, bool autoDispose )
		{
			if( host == null ) throw new ArgumentNullException( "host", "Host object to extend cannot be null." );
			if( !host.GetType().IsClass ) throw new InvalidOperationException( "Only classes can be extended." );
			if( propertyName == null ) throw new ArgumentNullException( "propertyName", "Property name cannot be null." );
			if( string.IsNullOrEmpty( propertyName ) ) throw new ArgumentException( "propertyName", "Property name cannot be empty." );

			lock( host ) {
				var holder = _GetMetaPropertiesHolder( host, true ); // Holder creation requested

				// If exists...
				MetaProperty meta = null; if( holder._MetaProperties.TryGetValue( propertyName, out meta ) ) {

					// Dispose the old value if needed...
					if( meta.PropertyValue != null && meta.PropertyValue is IDisposable && meta.AutoDispose )
						( (IDisposable)meta.PropertyValue ).Dispose();

					// And storing the new one...
					meta.PropertyValue = value;
					meta.AutoDispose = autoDispose;
				}
				// If not, create the meta-property...
				else {
					meta = new MetaProperty( propertyName, value, autoDispose );
					holder._MetaProperties.Add( propertyName, meta );
				}
			}
		}
		internal static T _GetMetaProperty<T>( object host, string propertyName )
		{
			if( host == null ) throw new ArgumentNullException( "host", "Host object to extend cannot be null." );
			if( !host.GetType().IsClass ) throw new InvalidOperationException( "Only classes can be extended." );
			if( propertyName == null ) throw new ArgumentNullException( "propertyName", "Property name cannot be null." );
			if( string.IsNullOrEmpty( propertyName ) ) throw new ArgumentException( "propertyName", "Property name cannot be empty." );

			lock( host ) {
				var holder = _GetMetaPropertiesHolder( host, false ); // Do not create holder
				if( holder == null ) throw new InvalidOperationException( "This host does not carry a meta-properties holder." );

				MetaProperty meta = null;
				if( !holder._MetaProperties.TryGetValue( propertyName, out meta ) )
					throw new ArgumentException( "This host does not carry the meta-property: " + propertyName );

				return (T)meta.PropertyValue;
			}
		}
		internal static bool _TryGetMetaProperty<T>( object host, string propertyName, out T? value ) where T : class
		{
			if( host == null ) throw new ArgumentNullException( "host", "Host object to extend cannot be null." );
			if( !host.GetType().IsClass ) throw new InvalidOperationException( "Only classes can be extended." );
			if( propertyName == null ) throw new ArgumentNullException( "propertyName", "Property name cannot be null." );
			if( string.IsNullOrEmpty( propertyName ) ) throw new ArgumentException( "propertyName", "Property name cannot be empty." );

			lock( host ) {
				var holder = _GetMetaPropertiesHolder( host, false ); // Do not create holder
				if( holder == null ) { value = null; return false; }

				MetaProperty meta = null;
				if( !holder._MetaProperties.TryGetValue( propertyName, out meta ) ) { value = null; return false; }

				value = meta.PropertyValue as T;
				return true;
			}
		}

		internal static bool _HasMetaProperty( object host, string propertyName )
		{
			if( host == null ) throw new ArgumentNullException( "host", "Host object to extend cannot be null." );
			if( !host.GetType().IsClass ) throw new InvalidOperationException( "Only classes can be extended." );
			if( propertyName == null ) throw new ArgumentNullException( "propertyName", "Property name cannot be null." );
			if( string.IsNullOrEmpty( propertyName ) ) throw new ArgumentException( "propertyName", "Property name cannot be empty." );

			lock( host ) {
				var holder = _GetMetaPropertiesHolder( host, false ); // Do not create holder
				if( holder == null ) return false;

				if( !holder._MetaProperties.ContainsKey( propertyName ) ) return false;
				return true;
			}
		}
		internal static MetaProperty _RemoveMetaProperty( object host, string propertyName )
		{
			if( host == null ) throw new ArgumentNullException( "host", "Host object to extend cannot be null." );
			if( !host.GetType().IsClass ) throw new InvalidOperationException( "Only classes can be extended." );
			if( propertyName == null ) throw new ArgumentNullException( "propertyName", "Property name cannot be null." );
			if( string.IsNullOrEmpty( propertyName ) ) throw new ArgumentException( "propertyName", "Property name cannot be empty." );

			lock( host ) {
				var holder = _GetMetaPropertiesHolder( host, false ); // Do not create holder
				if( holder == null ) return null;

				MetaProperty meta = null;
				if( !holder._MetaProperties.TryGetValue( propertyName, out meta ) )
					return null;

				holder._MetaProperties.Remove( propertyName );
				return meta;
			}
		}
		internal static void _ClearMetaProperties( object host )
		{
			if( host == null ) throw new ArgumentNullException( "host", "Host object to extend cannot be null." );
			if( !host.GetType().IsClass ) throw new InvalidOperationException( "Only classes can be extended." );

			lock( host ) {
				var holder = _GetMetaPropertiesHolder( host, false ); // Do not create holder
				if( holder != null ) holder.Clear();
			}
		}
		internal static List<string> _ListMetaProperties( object host )
		{
			if( host == null ) throw new ArgumentNullException( "host", "Host object to extend cannot be null." );
			if( !host.GetType().IsClass ) throw new InvalidOperationException( "Only classes can be extended." );

			List<string> list = new List<string>();
			lock( host ) {

				var holder = _GetMetaPropertiesHolder( host, false ); // Do not create holder
				if( holder != null ) {
					foreach( var kvp in holder._MetaProperties )
						list.Add( kvp.Key );
				}
			}
			return list;
		}
	}
}
// ========================================================
