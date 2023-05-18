#if HAS_EMOJI_SEARCH
using Kyub.EmojiSearch.UI;
using UnityEngine;
using static FlexibleTextMesh;

public class FlexibleEmojiTextMesh : TMP_EmojiTextUGUI
{	[SerializeField] bool _shrinkContent;
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

	protected override void GenerateTextMesh()
	{
		base.GenerateTextMesh();

		if (TransformMesh(this, _shrinkContent, _shrinkLineByLine, _curveType, _radius))
		{
			UpdateVertexData();
		}
	}
}
#endif
