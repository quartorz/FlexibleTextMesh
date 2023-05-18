using System;
using System.Collections.Generic;
using System.Globalization;
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

	static readonly StringBuilder _stringBuilder = new StringBuilder();
	static readonly List<int> _indices = new List<int>();

	static readonly Vector3[] _corners = new Vector3[4];

	protected override void GenerateTextMesh()
	{
		base.GenerateTextMesh();

		if (TransformMesh(this, _shrinkContent, _shrinkLineByLine, _curveType, _radius))
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

	public static bool TransformMesh(TMP_Text target, bool shrinkContent, bool shrinkLineByLine, CurveType curveType,
		float radius)
	{
		var vertexModified = false;

		if (shrinkContent)
		{
			var rectTransform = target.rectTransform;

			var width = rectTransform.rect.width;
			var textWidth = 0f;

			var textInfo = target.textInfo;

			for (var lineIndex = 0; lineIndex < textInfo.lineCount; ++lineIndex)
			{
				var lineInfo = textInfo.lineInfo[lineIndex];
				textWidth = Mathf.Max(textWidth, lineInfo.lineExtents.max.x - lineInfo.lineExtents.min.x);
			}

			var horizontalAlignment = target.horizontalAlignment;

			if (horizontalAlignment != HorizontalAlignmentOptions.Justified
				&& horizontalAlignment != HorizontalAlignmentOptions.Flush
				&& textWidth > width)
			{
				vertexModified = true;

				var lineVertexIndex = -1;
				var scale = width / textWidth;
				var pivotX = Mathf.LerpUnclamped(0, width, rectTransform.pivot.x);

				var font = target.font;
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

					float offset, offset2;

					switch (horizontalAlignment)
					{
						case HorizontalAlignmentOptions.Geometry:
						case HorizontalAlignmentOptions.Center:
							offset = -(lineInfo.lineExtents.min.x + lineInfo.lineExtents.max.x) / 2;
							offset2 = pivotX - width / 2;
							break;
						case HorizontalAlignmentOptions.Left:
							offset = -lineInfo.lineExtents.min.x;
							offset2 = pivotX;
							break;
						case HorizontalAlignmentOptions.Right:
							offset = -lineInfo.lineExtents.max.x;
							offset2 = pivotX - width;
							break;
						default:
							throw new Exception();
					}

					if (shrinkLineByLine)
					{
						textWidth = lineInfo.lineExtents.max.x - lineInfo.lineExtents.min.x;

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
							meshInfo.vertices[charInfo.vertexIndex + j].x += offset;
							meshInfo.vertices[charInfo.vertexIndex + j].x *= scale;
							meshInfo.vertices[charInfo.vertexIndex + j].x -= offset2;
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
								meshInfo.vertices[lineVertexIndex + j].x += offset;
								meshInfo.vertices[lineVertexIndex + j].x *= scale;
								meshInfo.vertices[lineVertexIndex + j].x -= offset2;
							}
						}

						if ((charInfo.style & FontStyles.Strikethrough) == FontStyles.Strikethrough &&
							charInfo.strikethroughVertexIndex != lineVertexIndex)
						{
							lineVertexIndex = charInfo.strikethroughVertexIndex;
							for (var j = 0; j < 12; ++j)
							{
								meshInfo.vertices[lineVertexIndex + j].x += offset;
								meshInfo.vertices[lineVertexIndex + j].x *= scale;
								meshInfo.vertices[lineVertexIndex + j].x -= offset2;
							}
						}
					}
				}
			}
		}

		if (curveType != CurveType.None)
		{
			vertexModified = true;

			if (curveType != CurveType.RotatePerGrapheme)
			{
				var textInfo = target.textInfo;
				for (var charIndex = 0; charIndex < textInfo.characterCount; ++charIndex)
				{
					var charInfo = textInfo.characterInfo[charIndex];

					if (!charInfo.isVisible)
					{
						continue;
					}

					var meshInfo = textInfo.meshInfo[charInfo.materialReferenceIndex];

					if (curveType == CurveType.RotatePerVertex)
					{
						for (var vertexCount = 0; vertexCount < 4; ++vertexCount)
						{
							var index = charInfo.vertexIndex + vertexCount;
							var v = meshInfo.vertices[index];

							var angle = v.x / radius;
							meshInfo.vertices[index] = new Vector3(Mathf.Sin(angle) * (v.y + radius),
								Mathf.Cos(angle) * (v.y + radius), v.z);
						}
					}
					else
					{
						var center = (meshInfo.vertices[charInfo.vertexIndex] +
							meshInfo.vertices[charInfo.vertexIndex + 1] +
							meshInfo.vertices[charInfo.vertexIndex + 2] +
							meshInfo.vertices[charInfo.vertexIndex + 3]) / 4f;

						var rad = center.x / radius;
						var sin = Mathf.Sin(-rad);
						var cos = Mathf.Cos(-rad);
						var centerRot = new Vector3(-sin * (center.y + radius), cos * (center.y + radius), center.z);

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
				var textInfo = target.textInfo;
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

						var min = new Vector2(float.MaxValue, float.MaxValue);
						var max = new Vector2(float.MinValue, float.MinValue);

						for (var charCount = 0; charCount < element.Length; ++charCount)
						{
							var charInfo = textInfo.characterInfo[_indices[enumerator.ElementIndex + charCount]];
							var meshInfo = textInfo.meshInfo[charInfo.materialReferenceIndex];
							for (var vertexCount = 0; vertexCount < 4; ++vertexCount)
							{
								min = Vector2.Min(min, meshInfo.vertices[charInfo.vertexIndex + vertexCount]);
								max = Vector2.Max(max, meshInfo.vertices[charInfo.vertexIndex + vertexCount]);
							}
						}

						Vector3 center = (min + max) / 2;

						var rad = -center.x / radius;
						var sin = Mathf.Sin(rad);
						var cos = Mathf.Cos(rad);
						var centerRot = new Vector2(-sin * (center.y + radius), cos * (center.y + radius));

						for (var charCount = 0; charCount < element.Length; ++charCount)
						{
							var charInfo = textInfo.characterInfo[_indices[enumerator.ElementIndex + charCount]];
							var meshInfo = textInfo.meshInfo[charInfo.materialReferenceIndex];

							for (var vertexCount = 0; vertexCount < 4; ++vertexCount)
							{
								var index = charInfo.vertexIndex + vertexCount;
								var v = meshInfo.vertices[index] - center;

								meshInfo.vertices[index] =
									centerRot + new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
							}
						}
					}
				}
			}
		}

		return vertexModified;
	}
}