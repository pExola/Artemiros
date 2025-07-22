using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;

[CustomEditor(typeof(LevelData))]
public class LevelDataEditor : Editor
{
    [System.Serializable]
    public struct ParPosicoes { public int x1, y1, x2, y2; }
    [System.Serializable]
    public struct TrioPosicoes { public int x1, y1, x2, y2, x3, y3; }

    private List<ParPosicoes> bisTemp = new List<ParPosicoes>();
    private List<TrioPosicoes> trisTemp = new List<TrioPosicoes>();
    private bool bisLoaded = false;
    private bool trisLoaded = false;

    public override void OnInspectorGUI()
    {
        LevelData data = (LevelData)target;

        data.TamanhoInventario = EditorGUILayout.IntField("Tamanho Inventário", data.TamanhoInventario);

        if (data.layoutDoGrid == null)
            data.layoutDoGrid = new List<LevelData.Linha>();

        int altura = data.layoutDoGrid.Count;
        int largura = (altura > 0 && data.layoutDoGrid[0].colunas != null) ? data.layoutDoGrid[0].colunas.Count : 0;

        int novaAltura = EditorGUILayout.IntField("Altura", altura);
        int novaLargura = EditorGUILayout.IntField("Largura", largura);
        if (novaAltura < 1) novaAltura = 1;
        if (novaLargura < 1) novaLargura = 1;

        while (data.layoutDoGrid.Count < novaAltura)
            data.layoutDoGrid.Add(new LevelData.Linha { colunas = new List<LevelData.TileConfig>() });
        while (data.layoutDoGrid.Count > novaAltura)
            data.layoutDoGrid.RemoveAt(data.layoutDoGrid.Count - 1);

        for (int y = 0; y < data.layoutDoGrid.Count; y++)
        {
            var linha = data.layoutDoGrid[y];
            if (linha.colunas == null)
                linha.colunas = new List<LevelData.TileConfig>();
            while (linha.colunas.Count < novaLargura)
                linha.colunas.Add(new LevelData.TileConfig());
            while (linha.colunas.Count > novaLargura)
                linha.colunas.RemoveAt(linha.colunas.Count - 1);
        }

        GUILayout.Label("Grid do Nível:", EditorStyles.boldLabel);
        for (int y = 0; y < data.layoutDoGrid.Count; y++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < novaLargura; x++)
            {
                var tile = data.layoutDoGrid[y].colunas[x];
                tile.tipo = (LevelData.TipoDeTile)EditorGUILayout.EnumPopup(tile.tipo, GUILayout.Width(60));
                if (tile.tipo == LevelData.TipoDeTile.Monstro)
                {
                    tile.corDoMonstro = EditorGUILayout.IntField(tile.corDoMonstro, GUILayout.Width(30));
                    tile.corDoMonstro = Mathf.Max(0, tile.corDoMonstro);
                }
                else
                {
                    tile.corDoMonstro = 0;
                }
                data.layoutDoGrid[y].colunas[x] = tile;
            }
            EditorGUILayout.EndHorizontal();
        }

        GUILayout.Space(10);
        GUILayout.Label("Bis (Monstros de 2 partes)", EditorStyles.boldLabel);
        if (!bisLoaded)
        {
            bisTemp.Clear();
            if (data.BisCoords != null)
            {
                foreach (var t in data.BisCoords)
                {
                    bisTemp.Add(new ParPosicoes {
                        x1 = t.x1, y1 = t.y1, x2 = t.x2, y2 = t.y2
                    });
                }
            }
            bisLoaded = true;
        }

        // Lista de Bis editável
        for (int i = 0; i < bisTemp.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Bis {i+1}", GUILayout.Width(40));
            bisTemp[i] = new ParPosicoes
            {
                x1 = EditorGUILayout.IntField(bisTemp[i].x1, GUILayout.Width(25)),
                y1 = EditorGUILayout.IntField(bisTemp[i].y1, GUILayout.Width(25)),
                x2 = EditorGUILayout.IntField(bisTemp[i].x2, GUILayout.Width(25)),
                y2 = EditorGUILayout.IntField(bisTemp[i].y2, GUILayout.Width(25)),
            };
            if (GUILayout.Button("Remover", GUILayout.Width(60)))
            {
                bisTemp.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }
        if (GUILayout.Button("Adicionar Bis"))
        {
            bisTemp.Add(new ParPosicoes());
        }

        GUILayout.Space(10);
        GUILayout.Label("Tris (Monstros de 3 partes)", EditorStyles.boldLabel);
        if (!trisLoaded)
        {
            trisTemp.Clear();
            if (data.TrisCoords != null)
            {
                foreach (var t in data.TrisCoords)
                {
                    trisTemp.Add(new TrioPosicoes {
                        x1 = t.x1, y1 = t.y1, x2 = t.x2, y2 = t.y2, x3 = t.x3, y3 = t.y3
                    });
                }
            }
            trisLoaded = true;
        }
        for (int i = 0; i < trisTemp.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Tri {i+1}", GUILayout.Width(40));
            trisTemp[i] = new TrioPosicoes
            {
                x1 = EditorGUILayout.IntField(trisTemp[i].x1, GUILayout.Width(25)),
                y1 = EditorGUILayout.IntField(trisTemp[i].y1, GUILayout.Width(25)),
                x2 = EditorGUILayout.IntField(trisTemp[i].x2, GUILayout.Width(25)),
                y2 = EditorGUILayout.IntField(trisTemp[i].y2, GUILayout.Width(25)),
                x3 = EditorGUILayout.IntField(trisTemp[i].x3, GUILayout.Width(25)),
                y3 = EditorGUILayout.IntField(trisTemp[i].y3, GUILayout.Width(25)),
            };
            if (GUILayout.Button("Remover", GUILayout.Width(60)))
            {
                trisTemp.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }
        if (GUILayout.Button("Adicionar Tri"))
        {
            trisTemp.Add(new TrioPosicoes());
        }

        if (GUI.changed)
        {
            // Salva as listas editadas de volta para o asset
            data.BisCoords = new List<LevelData.BisData>();
            foreach (var b in bisTemp)
                data.BisCoords.Add(new LevelData.BisData { x1 = b.x1, y1 = b.y1, x2 = b.x2, y2 = b.y2 });
            data.TrisCoords = new List<LevelData.TrisData>();
            foreach (var t in trisTemp)
                data.TrisCoords.Add(new LevelData.TrisData { x1 = t.x1, y1 = t.y1, x2 = t.x2, y2 = t.y2, x3 = t.x3, y3 = t.y3 });
            EditorUtility.SetDirty(data);
        }
    }
}
