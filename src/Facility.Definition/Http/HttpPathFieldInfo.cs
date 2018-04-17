using System.Collections.Generic;
using System.Linq;

namespace Facility.Definition.Http
{
	/// <summary>
	/// Information about a field that corresponds to a request path parameter.
	/// </summary>
	public sealed class HttpPathFieldInfo : HttpElementInfo
	{
		/// <summary>
		/// The service field.
		/// </summary>
		public ServiceFieldInfo ServiceField { get; }

		/// <summary>
		/// The name of the path parameter.
		/// </summary>
		public string Name => ServiceField.Name;

		/// <summary>
		/// The children of the element, if any.
		/// </summary>
		public override IEnumerable<HttpElementInfo> GetChildren() => Enumerable.Empty<HttpElementInfo>();

		internal HttpPathFieldInfo(ServiceFieldInfo fieldInfo)
		{
			ServiceField = fieldInfo;

			foreach (var parameter in GetHttpParameters(fieldInfo))
			{
				if (parameter.Name != "from")
					AddInvalidHttpParameterError(parameter);
			}
		}
	}
}
