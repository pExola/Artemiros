using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class LevelDataGridEditorWindow : EditorWindow
{
    private LevelData levelData;
    private LevelData lastLevelData = null;
    private Vector2 scroll;
    private (int x, int y)? firstSelected = null;
    private (int x, int y)? secondSelected = null;
    private (int x, int y)? thirdSelected = null;
    private List<LevelData.BisData> bisList = new List<LevelData.BisData>();
    private List<LevelData.TrisData> trisList = new List<LevelData.TrisData>();
    private string errorMsg = null;
    
    [MenuItem("Artemiros/Editor Visual de LevelData")]
    public static void ShowWindow()
    {
        GetWindow<LevelDataGridEditorWindow>("Editor Visual de LevelData");
    }

    private void OnGUI()
    {
        GUILayout.Label("Selecione o LevelData:", EditorStyles.boldLabel);
        LevelData newLevelData = (LevelData)EditorGUILayout.ObjectField(levelData, typeof(LevelData), false);
        if (newLevelData != levelData)
        {
            levelData = newLevelData;
            lastLevelData = levelData;
            SyncListsFromAsset();
            firstSelected = null;
            secondSelected = null;
            thirdSelected = null;
            errorMsg = null;
        }

        if (levelData == null)
        {
            EditorGUILayout.HelpBox("Selecione um LevelData válido.", MessageType.Info);
            return;
        }

        // Campo para editar o tamanho do inventário
        levelData.TamanhoInventario = EditorGUILayout.IntField("Tamanho do Inventário", levelData.TamanhoInventario);
        if (levelData.TamanhoInventario < 1) levelData.TamanhoInventario = 1;

        // --- Edição do Grid ---
        if (levelData.layoutDoGrid == null)
            levelData.layoutDoGrid = new List<LevelData.Linha>();
        int altura = levelData.layoutDoGrid.Count;
        int largura = (altura > 0 && levelData.layoutDoGrid[0].colunas != null) ? levelData.layoutDoGrid[0].colunas.Count : 0;

        GUILayout.Space(10);
        GUILayout.Label($"Grid: {largura} x {altura}", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Linha", GUILayout.Width(70)))
        {
            var novaLinha = new LevelData.Linha { colunas = new List<LevelData.TileConfig>() };
            for (int i = 0; i < largura; i++)
                novaLinha.colunas.Add(new LevelData.TileConfig());
            levelData.layoutDoGrid.Add(novaLinha);
            altura++;
        }
        if (altura > 1 && GUILayout.Button("- Linha", GUILayout.Width(70)))
        {
            levelData.layoutDoGrid.RemoveAt(altura - 1);
            altura--;
        }
        if (GUILayout.Button("+ Coluna", GUILayout.Width(80)))
        {
            for (int y = 0; y < altura; y++)
            {
                levelData.layoutDoGrid[y].colunas.Add(new LevelData.TileConfig());
            }
            largura++;
        }
        if (largura > 1 && GUILayout.Button("- Coluna", GUILayout.Width(80)))
        {
            for (int y = 0; y < altura; y++)
            {
                levelData.layoutDoGrid[y].colunas.RemoveAt(largura - 1);
            }
            largura--;
        }
        EditorGUILayout.EndHorizontal();

        // Ajusta o grid se necessário
        while (levelData.layoutDoGrid.Count < 1)
            levelData.layoutDoGrid.Add(new LevelData.Linha { colunas = new List<LevelData.TileConfig>() });
        for (int y = 0; y < levelData.layoutDoGrid.Count; y++)
        {
            if (levelData.layoutDoGrid[y].colunas == null)
                levelData.layoutDoGrid[y].colunas = new List<LevelData.TileConfig>();
            while (levelData.layoutDoGrid[y].colunas.Count < largura)
                levelData.layoutDoGrid[y].colunas.Add(new LevelData.TileConfig());
            while (levelData.layoutDoGrid[y].colunas.Count > largura)
                levelData.layoutDoGrid[y].colunas.RemoveAt(levelData.layoutDoGrid[y].colunas.Count - 1);
        }

        // --- Grid Visual e Edição Inline ---
        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(altura * 34 + 10));
        for (int y = 0; y < altura; y++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < largura; x++)
            {
                var tile = levelData.layoutDoGrid[y].colunas[x];
                string label = tile.tipo == LevelData.TipoDeTile.Monstro ? tile.corDoMonstro.ToString() : tile.tipo.ToString().Substring(0,1);
                bool isSelected = (firstSelected?.x == x && firstSelected?.y == y) || (secondSelected?.x == x && secondSelected?.y == y) || (thirdSelected?.x == x && thirdSelected?.y == y);
                bool isInTri = IsInTris(x, y);
                bool isInBi = !isInTri && IsInBis(x, y); // Prioriza azul para tris
                if (isInTri)
                    GUI.backgroundColor = new Color(0.5f, 0.7f, 1.0f); // azul claro
                else if (isInBi)
                    GUI.backgroundColor = Color.yellow;
                else if (isSelected)
                    GUI.backgroundColor = Color.green;
                else
                    GUI.backgroundColor = Color.white;
                EditorGUILayout.BeginVertical(GUILayout.Width(38));
                if (GUILayout.Button(label, GUILayout.Width(30), GUILayout.Height(30)))
                {
                    if (tile.tipo == LevelData.TipoDeTile.Monstro)
                    {
                        if (firstSelected == null)
                            firstSelected = (x, y);
                        else if (secondSelected == null && (firstSelected.Value.x != x || firstSelected.Value.y != y))
                            secondSelected = (x, y);
                        else if (thirdSelected == null && (firstSelected.Value.x != x || firstSelected.Value.y != y) && (secondSelected == null || (secondSelected.Value.x != x || secondSelected.Value.y != y)))
                            thirdSelected = (x, y);
                        else
                        {
                            firstSelected = (x, y);
                            secondSelected = null;
                            thirdSelected = null;
                        }
                    }
                }
                // Edição inline do tipo e cor
                tile.tipo = (LevelData.TipoDeTile)EditorGUILayout.EnumPopup(tile.tipo, GUILayout.Width(36));
                if (tile.tipo == LevelData.TipoDeTile.Monstro)
                {
                    tile.corDoMonstro = EditorGUILayout.IntField(tile.corDoMonstro, GUILayout.Width(36));
                    tile.corDoMonstro = Mathf.Max(0, tile.corDoMonstro);
                }
                else
                {
                    tile.corDoMonstro = 0;
                }
                levelData.layoutDoGrid[y].colunas[x] = tile;
                EditorGUILayout.EndVertical();
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        if (!string.IsNullOrEmpty(errorMsg))
        {
            EditorGUILayout.HelpBox(errorMsg, MessageType.Error);
        }

        GUILayout.Space(10);
        // Adicionar Bis
        if (firstSelected != null && secondSelected != null && thirdSelected == null)
        {
            GUILayout.Label($"Selecionado para Bis: ({firstSelected.Value.x},{firstSelected.Value.y}) e ({secondSelected.Value.x},{secondSelected.Value.y})");
            if (GUILayout.Button("Adicionar Bis"))
            {
                int cor1 = levelData.layoutDoGrid[firstSelected.Value.y].colunas[firstSelected.Value.x].corDoMonstro;
                int cor2 = levelData.layoutDoGrid[secondSelected.Value.y].colunas[secondSelected.Value.x].corDoMonstro;
                if (cor1 != cor2)
                {
                    errorMsg = "Os dois monstros do Bis devem ter a mesma cor!";
                }
                else if (!AreAdjacent(firstSelected.Value, secondSelected.Value))
                {
                    errorMsg = "Os dois monstros do Bis devem ser vizinhos (lado a lado)!";
                }
                else
                {
                    bisList.Add(new LevelData.BisData {
                        x1 = firstSelected.Value.x, y1 = firstSelected.Value.y,
                        x2 = secondSelected.Value.x, y2 = secondSelected.Value.y
                    });
                    firstSelected = null;
                    secondSelected = null;
                    errorMsg = null;
                }
            }
        }
        // Adicionar Tris
        if (firstSelected != null && secondSelected != null && thirdSelected != null)
        {
            GUILayout.Label($"Selecionado para Tri: ({firstSelected.Value.x},{firstSelected.Value.y}), ({secondSelected.Value.x},{secondSelected.Value.y}), ({thirdSelected.Value.x},{thirdSelected.Value.y})");
            if (GUILayout.Button("Adicionar Tri"))
            {
                int cor1 = levelData.layoutDoGrid[firstSelected.Value.y].colunas[firstSelected.Value.x].corDoMonstro;
                int cor2 = levelData.layoutDoGrid[secondSelected.Value.y].colunas[secondSelected.Value.x].corDoMonstro;
                int cor3 = levelData.layoutDoGrid[thirdSelected.Value.y].colunas[thirdSelected.Value.x].corDoMonstro;
                if (!(cor1 == cor2 && cor2 == cor3))
                {
                    errorMsg = "Os três monstros do Tri devem ter a mesma cor!";
                }
                else if (!AreAlignedAndConsecutive(firstSelected.Value, secondSelected.Value, thirdSelected.Value))
                {
                    errorMsg = "Os três monstros do Tri devem formar uma linha reta (vertical ou horizontal) e estar juntos!";
                }
                else
                {
                    trisList.Add(new LevelData.TrisData {
                        x1 = firstSelected.Value.x, y1 = firstSelected.Value.y,
                        x2 = secondSelected.Value.x, y2 = secondSelected.Value.y,
                        x3 = thirdSelected.Value.x, y3 = thirdSelected.Value.y
                    });
                    firstSelected = null;
                    secondSelected = null;
                    thirdSelected = null;
                    errorMsg = null;
                }
            }
        }

        GUILayout.Space(10);
        GUILayout.Label("Bis cadastrados:", EditorStyles.boldLabel);
        for (int i = 0; i < bisList.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Bis {i+1}: ({bisList[i].x1},{bisList[i].y1}) <-> ({bisList[i].x2},{bisList[i].y2})", GUILayout.Width(200));
            if (GUILayout.Button("Remover", GUILayout.Width(70)))
            {
                bisList.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }

        GUILayout.Space(10);
        GUILayout.Label("Tris cadastrados:", EditorStyles.boldLabel);
        for (int i = 0; i < trisList.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Tri {i+1}: ({trisList[i].x1},{trisList[i].y1}) - ({trisList[i].x2},{trisList[i].y2}) - ({trisList[i].x3},{trisList[i].y3})", GUILayout.Width(260));
            if (GUILayout.Button("Remover", GUILayout.Width(70)))
            {
                trisList.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }

        GUILayout.Space(10);
        if (GUILayout.Button("Salvar Bis/Tris no LevelData"))
        {
            SaveListsToAsset();
            EditorUtility.SetDirty(levelData);
            Debug.Log("Bis/Tris salvos por coordenadas!");
        }
    }

    private void SyncListsFromAsset()
    {
        bisList.Clear();
        trisList.Clear();
        if (levelData != null)
        {
            if (levelData.BisCoords != null)
                bisList.AddRange(levelData.BisCoords);
            if (levelData.TrisCoords != null)
                trisList.AddRange(levelData.TrisCoords);
        }
    }

    private void SaveListsToAsset()
    {
        if (levelData != null)
        {
            levelData.BisCoords = new List<LevelData.BisData>(bisList);
            levelData.TrisCoords = new List<LevelData.TrisData>(trisList);
        }
    }

    private bool IsInBis(int x, int y)
    {
        foreach (var b in bisList)
        {
            if ((b.x1 == x && b.y1 == y) || (b.x2 == x && b.y2 == y))
                return true;
        }
        return false;
    }
    private bool IsInTris(int x, int y)
    {
        foreach (var t in trisList)
        {
            if ((t.x1 == x && t.y1 == y) || (t.x2 == x && t.y2 == y) || (t.x3 == x && t.y3 == y))
                return true;
        }
        return false;
    }
    // Verifica se dois pontos são vizinhos (adjacentes)
    private bool AreAdjacent((int x, int y) a, (int x, int y) b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
    }
    // Verifica se três pontos estão alinhados e consecutivos
    private bool AreAlignedAndConsecutive((int x, int y) a, (int x, int y) b, (int x, int y) c)
    {
        // Ordena os pontos para facilitar a verificação
        var pts = new List<(int x, int y)> { a, b, c };
        pts.Sort((p1, p2) => p1.x != p2.x ? p1.x.CompareTo(p2.x) : p1.y.CompareTo(p2.y));
        // Horizontal
        if (pts[0].y == pts[1].y && pts[1].y == pts[2].y)
        {
            return pts[1].x == pts[0].x + 1 && pts[2].x == pts[1].x + 1;
        }
        // Vertical
        if (pts[0].x == pts[1].x && pts[1].x == pts[2].x)
        {
            return pts[1].y == pts[0].y + 1 && pts[2].y == pts[1].y + 1;
        }
        return false;
    }
}
