﻿using System;
using System.Collections.Generic;
using System.IO;

namespace Mustache
{
    /// <summary>
    /// Defines a tag that outputs the current index within an each loop.
    /// </summary>
    internal sealed class Value : InlineTagDefinition
    {
        /// <summary>
        /// Initializes a new instance of an IndexTagDefinition.
        /// </summary>
        public Value()
            : base("value", true)
        {
        }

        /// <summary>
        /// Gets the text to output.
        /// </summary>
        /// <param name="writer">The writer to write the output to.</param>
        /// <param name="arguments">The arguments passed to the tag.</param>
        /// <param name="contextScope">Extra data passed along with the context.</param>
        public override void GetText(TextWriter writer, Dictionary<string, object> arguments, Scope contextScope)
        {
            object val;
            if (contextScope.TryFind("value", out val))
            {
                writer.Write(val as string);
            }
        }
    }
}
