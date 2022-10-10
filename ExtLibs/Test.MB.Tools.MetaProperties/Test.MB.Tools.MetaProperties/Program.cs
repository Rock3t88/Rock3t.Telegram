// #undef DEBUG
// ========================================================
namespace Test.MB.Tools.MetaProperties
{
	using global::System;
	using global::System.Diagnostics;
	using global::System.Collections.Generic;
	using global::MB.Tools;

	// =====================================================
	class Program
	{
		// An example of a host class (with debug traces for following the example)
		public class My_Class : IDisposable
		{
			public string ClassID { get; private set; }
			public override string ToString()
			{
				return string.Format( "Class:{0}", ClassID ?? "<null>" );
			}

			public My_Class( string id )
			{
				Debug.Indent();
				Debug.WriteLine( string.Format( "=My_Class::( id:{0} )", id ?? "<null>" ) );
				try {
					ClassID = id;
				}
				finally { Debug.Unindent(); }
			}

			protected virtual void Dispose( bool disposing )
			{
				Debug.Indent();
				Debug.WriteLine( string.Format( "=My_Class[ {0} ]::Dispose( disposing:{1} )", this.ToString(), disposing ) );
				try {
				}
				finally { Debug.Unindent(); }
			}
			public void Dispose()
			{
				Debug.Indent();
				Debug.WriteLine( string.Format( "=My_Class[ {0} ]::Dispose()", this.ToString() ) );
				try {
					Dispose( true );
					GC.SuppressFinalize( this );
				}
				finally { Debug.Unindent(); }
			}
			~My_Class()
			{
				Debug.Indent();
				Debug.WriteLine( string.Format( "=My_Class[ {0} ]::~()", this.ToString() ) );
				try {
					Dispose( false );
				}
				finally { Debug.Unindent(); }
			}
		}

		// An example of a property class (with debug traces for following the example)
		public class My_Property : IDisposable
		{
			public string PropertyID { get; private set; }
			public override string ToString()
			{
				return string.Format( "Property:{0}", PropertyID ?? "<null>" );
			}

			public My_Property( string id )
			{
				Debug.Indent();
				Debug.WriteLine( string.Format( "=My_Property::( id:{0} )", id ?? "<null>" ) );
				try {
					PropertyID = id;
				}
				finally { Debug.Unindent(); }
			}

			protected virtual void Dispose( bool disposing )
			{
				Debug.Indent();
				Debug.WriteLine( string.Format( "=My_Property[ {0} ]::Dispose( disposing:{1} )", this.ToString(), disposing ) );
				try {
				}
				finally { Debug.Unindent(); }
			}
			public void Dispose()
			{
				Debug.Indent();
				Debug.WriteLine( string.Format( "=My_Property[ {0} ]::Dispose()", this.ToString() ) );
				try {
					Dispose( true );
					GC.SuppressFinalize( this );
				}
				finally { Debug.Unindent(); }
			}
			~My_Property()
			{
				Debug.Indent();
				Debug.WriteLine( string.Format( "=My_Property[ {0} ]::~()", this.ToString() ) );
				try {
					Dispose( false );
				}
				finally { Debug.Unindent(); }
			}
		}

		// The example...
		static void Example()
		{
			MetaPropertyExtender.StartCollector( 2000 ); // Short interval for the example.

			Console.WriteLine( "\n-Creating memory pressure..." );
			long memory = 0x7fffffffL;
			GC.AddMemoryPressure( memory );

			Console.Write( "\n-Press [Enter] to create the hosts and the meta-properties...\n" ); Console.ReadLine();
			new Action( () => {
				List<WeakReference> list = new List<WeakReference>(); // To store the created host instances...

				int num = 5; // Doing this a number of times...
				for( int n = 0; n < num; n++ ) {
					var host = new My_Class( "Peter_" + n );
					list.Add( new WeakReference( host ) );

					MetaPropertyExtender.SetMetaProperty( host, "FamilyName", new My_Property( "Smith_" + n ) ); // setting
					object value = MetaPropertyExtender.GetMetaProperty( host, "FamilyName" ); // getting
					Console.WriteLine( "\t\t ==> {0} {1}", host.ToString(), value.ToString() );
					Console.WriteLine();
				}

				Console.Write( "\n-Press [Enter] to dispose the hosts...\n" ); Console.ReadLine();
				for( int n = 0; n < num; n++ ) {
					if( list[n].Target != null && list[n].IsAlive )
						( (IDisposable)( list[n].Target ) ).Dispose();
				}
				list.Clear();

				Console.Write( "\n-Press [Enter] to start first GC collection...\n" ); Console.ReadLine();
				GC.Collect();
				GC.WaitForPendingFinalizers();

			} )();

			Console.Write( "\n-Press [Enter] to start second GC collection...\n" ); Console.ReadLine();
			GC.Collect();
			GC.WaitForPendingFinalizers();

			Console.Write( "\n-Press [Enter] to remove memory pressure...\n" ); Console.ReadLine();
			GC.RemoveMemoryPressure( memory );
		}

		static void Main( string[] args )
		{
			// Printing the traces in the console...
			TextWriterTraceListener tr = new TextWriterTraceListener( System.Console.Out );
			Debug.Listeners.Add( tr ); Debug.AutoFlush = true;

			Console.Write( "\n-Press [Enter] to begin...\n" ); Console.ReadLine();
			try { Example(); }
			#region Exceptions
			catch( Exception e ) {
				bool inner = false; while( e != null ) {
					Console.WriteLine( "\n---------------" );
					Console.WriteLine( inner ? "Inner Exception: {0}\n" : "Exception: {0}\n", e.Message );
					Console.WriteLine( e.StackTrace );
					e = e.InnerException; inner = true;
				}
			}
			#endregion
			Console.Write( "\n-Press [Enter] to terminate...\n" ); Console.ReadLine();
		}
	}
}
// ========================================================
