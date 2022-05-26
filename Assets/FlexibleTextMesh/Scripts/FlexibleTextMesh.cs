using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

public class FlexibleTextMesh : TextMeshProUGUI
{
	public enum CurveType
	{
		None,
		RotatePerVertex,
		RotatePerMesh,
		RotatePerGrapheme,
	}

	[SerializeField] bool _shrinkContent;
	[SerializeField] bool _shrinkLineByLine;
	[SerializeField] CurveType _curveType;
	[SerializeField] float _radius;

	public bool shrinkContent
	{
		get => _shrinkContent;
		set
		{
			_shrinkContent = value;
			SetAllDirty();
		}
	}

	public bool shrinkLineByLine
	{
		get => _shrinkLineByLine;
		set
		{
			_shrinkLineByLine = value;
			SetAllDirty();
		}
	}

	public CurveType curveType
	{
		get => _curveType;
		set
		{
			_curveType = value;
			SetAllDirty();
		}
	}

	public float radius
	{
		get => _radius;
		set
		{
			_radius = value;
			SetAllDirty();
		}
	}

	readonly StringBuilder _stringBuilder = new StringBuilder();
	readonly List<int> _indices = new List<int>();

	protected override void GenerateTextMesh()
	{
		base.GenerateTextMesh();

		var vertexModified = false;

		if (_shrinkContent)
		{
			var width = rectTransform.rect.width;
			var textWidth = 0f;

			for (var lineIndex = 0; lineIndex < textInfo.lineCount; ++lineIndex)
			{
				textWidth = Mathf.Max(textWidth, textInfo.lineInfo[lineIndex].length);
			}

			if (horizontalAlignment != HorizontalAlignmentOptions.Justified
				&& horizontalAlignment != HorizontalAlignmentOptions.Flush)
			{
				if (textWidth > width)
				{
					vertexModified = true;

					var lineVertexIndex = -1;

					var scaleX = width / textWidth;

					var xOffset = horizontalAlignment switch
					{
						HorizontalAlignmentOptions.Geometry => 0f,
						HorizontalAlignmentOptions.Left => width / 2,
						HorizontalAlignmentOptions.Center => 0f,
						HorizontalAlignmentOptions.Right => -width / 2,
						_ => 0f
					};

					var matrix = new Matrix4x4();

					if (!_shrinkLineByLine)
					{
						matrix = Matrix4x4.Translate(new Vector3(-xOffset, 0, 0))
							* Matrix4x4.Scale(new Vector3(scaleX, 1, 1))
							* Matrix4x4.Translate(new Vector3(xOffset, 0, 0));
					}

					var hasUnderline = font.HasCharacter('_');

					for (var lineIndex = 0; lineIndex < textInfo.lineCount; ++lineIndex)
					{
						var lineInfo = textInfo.lineInfo[lineIndex];

						if (_shrinkLineByLine)
						{
							textWidth = lineInfo.length;

							if (textWidth < width)
							{
								continue;
							}

							scaleX = width / textWidth;
							xOffset = ((int)alignment & 0b111) switch
							{
								0b000 => 0f, // GeoAligned
								0b001 => width / 2, // Left
								0b010 => 0f, // Center
								0b100 => -width / 2, // Right
								_ => throw new Exception("bug")
							};
							matrix = Matrix4x4.Translate(new Vector3(-xOffset, 0, 0))
								* Matrix4x4.Scale(new Vector3(scaleX, 1, 1))
								* Matrix4x4.Translate(new Vector3(xOffset, 0, 0));
						}

						for (var charIndex = lineInfo.firstCharacterIndex;
							charIndex <= lineInfo.lastCharacterIndex;
							++charIndex)
						{
							var charInfo = textInfo.characterInfo[charIndex];
							if (!charInfo.isVisible)
							{
								continue;
							}

							var meshInfo = textInfo.meshInfo[charInfo.materialReferenceIndex];
							for (var j = 0; j < 4; ++j)
							{
								meshInfo.vertices[charInfo.vertexIndex + j] =
									matrix.MultiplyPoint(meshInfo.vertices[charInfo.vertexIndex + j]);
							}

							if (!hasUnderline)
							{
								continue;
							}

							if ((charInfo.style & FontStyles.Underline) == FontStyles.Underline &&
								charInfo.underlineVertexIndex != lineVertexIndex)
							{
								lineVertexIndex = charInfo.underlineVertexIndex;
								for (var j = 0; j < 12; ++j)
								{
									meshInfo.vertices[lineVertexIndex + j] =
										matrix.MultiplyPoint(meshInfo.vertices[lineVertexIndex + j]);
								}
							}

							if ((charInfo.style & FontStyles.Strikethrough) == FontStyles.Strikethrough &&
								charInfo.strikethroughVertexIndex != lineVertexIndex)
							{
								lineVertexIndex = charInfo.strikethroughVertexIndex;
								for (var j = 0; j < 12; ++j)
								{
									meshInfo.vertices[lineVertexIndex + j] =
										matrix.MultiplyPoint(meshInfo.vertices[lineVertexIndex + j]);
								}
							}
						}
					}
				}
			}
		}

		if (_curveType != CurveType.None)
		{
			vertexModified = true;

			if (_curveType != CurveType.RotatePerGrapheme)
			{
				for (var charIndex = 0; charIndex < textInfo.characterCount; ++charIndex)
				{
					var charInfo = textInfo.characterInfo[charIndex];

					if (!charInfo.isVisible)
					{
						continue;
					}

					var meshInfo = textInfo.meshInfo[charInfo.materialReferenceIndex];

					if (_curveType == CurveType.RotatePerVertex)
					{
						for (var vertexCount = 0; vertexCount < 4; ++vertexCount)
						{
							var index = charInfo.vertexIndex + vertexCount;
							var v = meshInfo.vertices[index];

							var angle = v.x / _radius;
							meshInfo.vertices[index] = new Vector3(Mathf.Sin(angle) * (v.y + _radius),
								Mathf.Cos(angle) * (v.y + _radius), v.z);
						}
					}
					else
					{
						var center = (meshInfo.vertices[charInfo.vertexIndex] +
							meshInfo.vertices[charInfo.vertexIndex + 1] +
							meshInfo.vertices[charInfo.vertexIndex + 2] +
							meshInfo.vertices[charInfo.vertexIndex + 3]) / 4f;

						var rad = center.x / _radius;
						var sin = Mathf.Sin(-rad);
						var cos = Mathf.Cos(-rad);
						var centerRot = new Vector3(-sin * (center.y + _radius), cos * (center.y + _radius), center.z);

						for (var vertexCount = 0; vertexCount < 4; ++vertexCount)
						{
							var index = charInfo.vertexIndex + vertexCount;
							var v = meshInfo.vertices[index] - center;

							meshInfo.vertices[index] =
								centerRot + new Vector3(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
						}
					}
				}
			}
			else
			{
				for (var lineCount = 0; lineCount < textInfo.lineCount; ++lineCount)
				{
					var lineInfo = textInfo.lineInfo[lineCount];

					_stringBuilder.Clear();
					_stringBuilder.EnsureCapacity(lineInfo.visibleCharacterCount);

					_indices.Clear();
					_indices.Capacity = Mathf.Max(_indices.Capacity, lineInfo.visibleCharacterCount);

					for (var j = lineInfo.firstCharacterIndex; j <= lineInfo.lastCharacterIndex; ++j)
					{
						var charInfo = textInfo.characterInfo[j];
						if (!charInfo.isVisible)
						{
							continue;
						}

						_indices.Add(j);
						_stringBuilder.Append(charInfo.character);
					}

					var enumerator = StringInfo.GetTextElementEnumerator(_stringBuilder.ToString());

					while (enumerator.MoveNext())
					{
						var element = enumerator.GetTextElement();

						var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
						var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

						for (var charCount = 0; charCount < element.Length; ++charCount)
						{
							var charInfo = textInfo.characterInfo[_indices[enumerator.ElementIndex + charCount]];
							var meshInfo = textInfo.meshInfo[charInfo.materialReferenceIndex];
							for (var vertexCount = 0; vertexCount < 4; ++vertexCount)
							{
								min = Vector3.Min(min, meshInfo.vertices[charInfo.vertexIndex + vertexCount]);
								max = Vector3.Max(max, meshInfo.vertices[charInfo.vertexIndex + vertexCount]);
							}
						}

						var center = (min + max) / 2;

						var rad = -center.x / _radius;
						var sin = Mathf.Sin(rad);
						var cos = Mathf.Cos(rad);
						var centerRot = new Vector3(-sin * (center.y + _radius), cos * (center.y + _radius), center.z);

						for (var charCount = 0; charCount < element.Length; ++charCount)
						{
							var charInfo = textInfo.characterInfo[_indices[enumerator.ElementIndex + charCount]];
							var meshInfo = textInfo.meshInfo[charInfo.materialReferenceIndex];

							for (var vertexCount = 0; vertexCount < 4; ++vertexCount)
							{
								var index = charInfo.vertexIndex + vertexCount;
								var v = meshInfo.vertices[index] - center;

								meshInfo.vertices[index] =
									centerRot + new Vector3(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
							}
						}
					}
				}
			}
		}

		if (vertexModified)
		{
			UpdateVertexData();
		}
	}
}