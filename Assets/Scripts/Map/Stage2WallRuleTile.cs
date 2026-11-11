using System;
using UnityEngine;
using UnityEngine.Tilemaps;

// Stage 2 돌 벽용 Rule Tile.
// Tile Palette 에서 이 타일 하나로 칠하면 상하좌우 이웃을 보고 외곽선(모서리/중앙) 타일이,
// 대각선 이웃을 보고 안쪽 꼭짓점 타일이 자동으로 바뀜.
// treatAsWall: 여기에 넣은 다른 타일도 "벽"으로 취급 (필요할 때만 사용)
// 에셋: Assets/Picture/2Stage_Tile/Stage2_Wall.asset (Mousword > Stage 2 > Build All 이 자동 생성)
public class Stage2WallRuleTile : RuleTile
{
    public TileBase[] treatAsWall = Array.Empty<TileBase>();

    public override bool RuleMatch(int neighbor, TileBase other)
    {
        bool isWall = other == this || (other != null && Array.IndexOf(treatAsWall, other) >= 0);
        switch (neighbor)
        {
            case TilingRuleOutput.Neighbor.This: return isWall;
            case TilingRuleOutput.Neighbor.NotThis: return !isWall;
        }
        return base.RuleMatch(neighbor, other);
    }
}
