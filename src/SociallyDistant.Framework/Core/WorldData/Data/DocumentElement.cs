using SociallyDistant.Core.Core.Serialization;

namespace SociallyDistant.Core.Core.WorldData.Data
{
	public struct DocumentElement : IWorldData
	{
		private byte                  elementType;
		private string                data;
		private List<DocumentElement> subDocuments;
		
		/// <inheritdoc />
		public void Serialize(IWorldSerializer serializer)
		{
			SerializationUtility.SerializeAtRevision(ref elementType, serializer, WorldRevision.Begin, default);
			SerializationUtility.SerializeAtRevision(ref data,        serializer, WorldRevision.Begin, string.Empty);

			if (serializer.IsReading)
			{
				this.subDocuments = new List<DocumentElement>();

				int count = 0;
				SerializationUtility.SerializeAtRevision(ref count, serializer, WorldRevision.SubDocuments, 0);

				for (var i = 0; i < count; i++)
				{
					var element = new DocumentElement();
					element.Serialize(serializer);
					subDocuments.Add(element);
				}
			}
			else
			{
				int count = subDocuments?.Count ?? 0;
				
				SerializationUtility.SerializeAtRevision(ref count, serializer, WorldRevision.SubDocuments, 0);

				for (var i = 0; i < count; i++)
				{
					// Note: If we got a count other than zero, the collection can't possibly be null.
					DocumentElement element = subDocuments![i];
					element.Serialize(serializer);
				}
			}
		}

		public void Write(IDataWriter writer)
		{
			writer.Write(elementType);
			writer.Write(data);

			uint elementCount = (uint)(subDocuments?.Count ?? 0);
			writer.Write(elementCount);
			
			for (var i = 0; i < elementCount; i++)
			{
				var doc = subDocuments![i];
				doc.Write(writer);
				subDocuments.Add(doc);
			}
		}

		
		public void Read(IDataReader reader)
		{
			elementType = reader.Read_byte();
			data = reader.Read_string();

			uint elementCount = reader.Read_uint();

			this.subDocuments = new List<DocumentElement>();
			for (var i = 0; i < elementCount; i++)
			{
				var doc = new DocumentElement();
				doc.Read(reader);
				subDocuments.Add(doc);
			}
		}
			
		public DocumentElementType ElementType
		{
			get => (DocumentElementType) elementType;
			set => elementType = (byte) value;
		}
		
		public string Data
		{
			get => data;
			set => data = value;
		}

		public static bool operator ==(DocumentElement a, DocumentElement b)
		{
			return (a.ElementType == b.ElementType && a.Data == b.Data);
		}

		public static bool operator !=(DocumentElement a, DocumentElement b)
		{
			return !(a == b);
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			return obj is DocumentElement element && element == this;
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			return HashCode.Combine(ElementType, Data);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"(ElementType={ElementType}, Data={Data})";
		}

		public IReadOnlyList<DocumentElement> Children
		{
			get
			{
				if (subDocuments == null)
					subDocuments = new();
				return subDocuments;
			}
			set => subDocuments = value.ToList();
		}
	}
}