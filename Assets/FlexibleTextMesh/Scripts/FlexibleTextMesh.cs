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

	readonly Vector3[] _corners = new Vector3[4];

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

				var lineInfo = textInfo.lineInfo[lineIndex];
				textWidth = Mathf.Max(textWidth, lineInfo.lineExtents.max.x - lineInfo.lineExtents.min.x);
			}

			if (horizontalAlignment != HorizontalAlignmentOptions.Justified
				&& horizontalAlignment != HorizontalAlignmentOptions.Flush
				&& textWidth > width)
			{
				vertexModified = true;

				var lineVertexIndex = -1;
				var scale = width / textWidth;
				var pivot = rectTransform.pivot;

				var hasUnderline = font.HasCharacter('_');

				if (horizontalAlignment == HorizontalAlignmentOptions.Left
					|| horizontalAlignment == HorizontalAlignmentOptions.Right)
				{
					rectTransform.GetLocalCorners(_corners);
				}

				for (var lineIndex = 0; lineIndex < textInfo.lineCount; ++lineIndex)
				{
					var lineInfo = textInfo.lineInfo[lineIndex];

					if (lineInfo.visibleCharacterCount == 0)
					{
						continue;
					}

					float offset, diff;

					switch (horizontalAlignment)
					{
						case HorizontalAlignmentOptions.Geometry:
						case HorizontalAlignmentOptions.Center:
							offset = diff = 0;
							break;
						case HorizontalAlignmentOptions.Left:
							offset = Mathf.LerpUnclamped(0, width, pivot.x);
							diff = _corners[0].x - lineInfo.lineExtents.min.x;
							break;
						case HorizontalAlignmentOptions.Right:
							offset = -Mathf.LerpUnclamped(0, width, pivot.x);
							diff = _corners[2].x - lineInfo.lineExtents.max.x;
							break;
						default:
							throw new Exception();
					}

					if (_shrinkLineByLine)
					{
						textWidth = lineInfo.length;

						if (textWidth < width)
						{
							continue;
						}

						scale = width / textWidth;
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
							meshInfo.vertices[charInfo.vertexIndex + j].x += offset + diff;
							meshInfo.vertices[charInfo.vertexIndex + j].x *= scale;
							meshInfo.vertices[charInfo.vertexIndex + j].x -= offset;
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
								meshInfo.vertices[lineVertexIndex + j].x += offset + diff;
								meshInfo.vertices[lineVertexIndex + j].x *= scale;
								meshInfo.vertices[lineVertexIndex + j].x -= offset;
							}
						}

						if ((charInfo.style & FontStyles.Strikethrough) == FontStyles.Strikethrough &&
							charInfo.strikethroughVertexIndex != lineVertexIndex)
						{
							lineVertexIndex = charInfo.strikethroughVertexIndex;
							for (var j = 0; j < 12; ++j)
							{
								meshInfo.vertices[lineVertexIndex + j].x += offset + diff;
								meshInfo.vertices[lineVertexIndex + j].x *= scale;
								meshInfo.vertices[lineVertexIndex + j].x -= offset;
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

	void OnDrawGizmosSelected()
	{
		if (_curveType == CurveType.None)
		{
			return;
		}

		rectTransform.GetLocalCorners(_corners);

		var matrix = rectTransform.localToWorldMatrix;

		var min = Vector3.Min(_corners[0], _corners[2]);
		var max = Vector3.Max(_corners[0], _corners[2]);
		var center = new Vector3(
			Mathf.LerpUnclamped(min.x, max.x, rectTransform.pivot.x),
			Mathf.LerpUnclamped(min.y, max.y, rectTransform.pivot.y), 0);

		var top = center + new Vector3(0, _radius, 0);

		var to = matrix.MultiplyPoint(top);

		var count = Mathf.Floor(_radius);
		for (var i = 0; i <= count; ++i)
		{
			var from = to;

			var angle = i * Mathf.PI * 2 / count;
			to = matrix.MultiplyPoint(
				new Vector3(
					Mathf.Sin(angle) * _radius,
					Mathf.Cos(angle) * _radius) + center);
			Gizmos.DrawLine(from, to);
		}
	}
}