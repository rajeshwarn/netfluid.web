using System;
using System.Collections.Generic;

namespace Netfluid.DB
{
	internal class TreeKeyExistsException : Exception
	{
		public TreeKeyExistsException (object key) : base ("Duplicate key: " + key.ToString())
		{
			
		}
	}

}

