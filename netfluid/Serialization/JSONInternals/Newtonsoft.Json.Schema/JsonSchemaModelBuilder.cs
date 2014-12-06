using System;
using System.Collections.Generic;
using System.Linq;
namespace Newtonsoft.Json.Schema
{
	internal class JsonSchemaModelBuilder
	{
		private JsonSchemaNodeCollection _nodes = new JsonSchemaNodeCollection();
		private Dictionary<JsonSchemaNode, JsonSchemaModel> _nodeModels = new Dictionary<JsonSchemaNode, JsonSchemaModel>();
		private JsonSchemaNode _node;
		internal JsonSchemaModel Build(JsonSchema schema)
		{
			this._nodes = new JsonSchemaNodeCollection();
			this._node = this.AddSchema(null, schema);
			this._nodeModels = new Dictionary<JsonSchemaNode, JsonSchemaModel>();
			return this.BuildNodeModel(this._node);
		}
		internal JsonSchemaNode AddSchema(JsonSchemaNode existingNode, JsonSchema schema)
		{
			string newId;
			if (existingNode != null)
			{
				if (existingNode.Schemas.Contains(schema))
				{
					return existingNode;
				}
				newId = JsonSchemaNode.GetId(existingNode.Schemas.Union(new JsonSchema[]
				{
					schema
				}));
			}
			else
			{
				newId = JsonSchemaNode.GetId(new JsonSchema[]
				{
					schema
				});
			}
			if (this._nodes.Contains(newId))
			{
				return this._nodes[newId];
			}
			JsonSchemaNode currentNode = (existingNode != null) ? existingNode.Combine(schema) : new JsonSchemaNode(schema);
			this._nodes.Add(currentNode);
			this.AddProperties(schema.Properties, currentNode.Properties);
			this.AddProperties(schema.PatternProperties, currentNode.PatternProperties);
			if (schema.Items != null)
			{
				for (int i = 0; i < schema.Items.Count; i++)
				{
					this.AddItem(currentNode, i, schema.Items[i]);
				}
			}
			if (schema.AdditionalItems != null)
			{
				this.AddAdditionalItems(currentNode, schema.AdditionalItems);
			}
			if (schema.AdditionalProperties != null)
			{
				this.AddAdditionalProperties(currentNode, schema.AdditionalProperties);
			}
			if (schema.Extends != null)
			{
				foreach (JsonSchema jsonSchema in schema.Extends)
				{
					currentNode = this.AddSchema(currentNode, jsonSchema);
				}
			}
			return currentNode;
		}
		internal void AddProperties(IDictionary<string, JsonSchema> source, IDictionary<string, JsonSchemaNode> target)
		{
			if (source != null)
			{
				foreach (KeyValuePair<string, JsonSchema> property in source)
				{
					this.AddProperty(target, property.Key, property.Value);
				}
			}
		}
		internal void AddProperty(IDictionary<string, JsonSchemaNode> target, string propertyName, JsonSchema schema)
		{
			JsonSchemaNode propertyNode;
			target.TryGetValue(propertyName, out propertyNode);
			target[propertyName] = this.AddSchema(propertyNode, schema);
		}
		internal void AddItem(JsonSchemaNode parentNode, int index, JsonSchema schema)
		{
			JsonSchemaNode existingItemNode = (parentNode.Items.Count > index) ? parentNode.Items[index] : null;
			JsonSchemaNode newItemNode = this.AddSchema(existingItemNode, schema);
			if (parentNode.Items.Count <= index)
			{
				parentNode.Items.Add(newItemNode);
				return;
			}
			parentNode.Items[index] = newItemNode;
		}
		internal void AddAdditionalProperties(JsonSchemaNode parentNode, JsonSchema schema)
		{
			parentNode.AdditionalProperties = this.AddSchema(parentNode.AdditionalProperties, schema);
		}
		internal void AddAdditionalItems(JsonSchemaNode parentNode, JsonSchema schema)
		{
			parentNode.AdditionalItems = this.AddSchema(parentNode.AdditionalItems, schema);
		}
		private JsonSchemaModel BuildNodeModel(JsonSchemaNode node)
		{
			JsonSchemaModel model;
			if (this._nodeModels.TryGetValue(node, out model))
			{
				return model;
			}
			model = JsonSchemaModel.Create(node.Schemas);
			this._nodeModels[node] = model;
			foreach (KeyValuePair<string, JsonSchemaNode> property in node.Properties)
			{
				if (model.Properties == null)
				{
					model.Properties = new Dictionary<string, JsonSchemaModel>();
				}
				model.Properties[property.Key] = this.BuildNodeModel(property.Value);
			}
			foreach (KeyValuePair<string, JsonSchemaNode> property2 in node.PatternProperties)
			{
				if (model.PatternProperties == null)
				{
					model.PatternProperties = new Dictionary<string, JsonSchemaModel>();
				}
				model.PatternProperties[property2.Key] = this.BuildNodeModel(property2.Value);
			}
			foreach (JsonSchemaNode t in node.Items)
			{
				if (model.Items == null)
				{
					model.Items = new List<JsonSchemaModel>();
				}
				model.Items.Add(this.BuildNodeModel(t));
			}
			if (node.AdditionalProperties != null)
			{
				model.AdditionalProperties = this.BuildNodeModel(node.AdditionalProperties);
			}
			if (node.AdditionalItems != null)
			{
				model.AdditionalItems = this.BuildNodeModel(node.AdditionalItems);
			}
			return model;
		}
	}
}
