using System;

namespace Netfluid.Db
{
	internal class TreeNodeSerializationException : Exception
	{
		public TreeNodeSerializationException (Exception innerException) 
			: base ("Failed to serialize/deserialize heat map node", innerException)
		{
			
		}
	}
}

