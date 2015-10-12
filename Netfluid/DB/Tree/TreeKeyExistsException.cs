using System;
using System.Collections.Generic;

namespace Netfluid.Db
{
	internal class TreeKeyExistsException : Exception
	{
		public TreeKeyExistsException (object key) : base ("Duplicate key: " + key.ToString())
		{
			
		}
	}

}

