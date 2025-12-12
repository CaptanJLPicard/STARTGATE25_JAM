using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

public class ReadyListUI : MonoBehaviour
{
    [SerializeField] private FusionManager fusion;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private ReadyRow rowPrefab;

    readonly Dictionary<PlayerRef, ReadyRow> rows = new();

    private void Awake()
    {
        if (!fusion) fusion = FindFirstObjectByType<FusionManager>(FindObjectsInactive.Include);
    }

    private void Update()
    {
        var runner = fusion ? fusion.Runner : null;
        if (!runner) return;

        var current = runner.ActivePlayers.ToList();

        // Çýkan oyuncularý UI'dan sil
        var toRemove = rows.Keys.Where(k => !current.Contains(k)).ToList();
        foreach (var k in toRemove)
        {
            Destroy(rows[k].gameObject);
            rows.Remove(k);
        }

        // Yeni oyuncular için satýr ekle
        foreach (var p in current)
        {
            if (rows.ContainsKey(p)) continue;
            var obj = runner.GetPlayerObject(p);
            var pl = obj ? obj.GetComponent<Player>() : null;
            if (!pl) continue;

            var row = Instantiate(rowPrefab, contentRoot);

            // --- ÖNEMLÝ KISIM: En üstte gözüksün ---
            row.transform.SetAsFirstSibling();
            // veya:
            // row.transform.SetSiblingIndex(0);

            row.Bind(pl);
            rows.Add(p, row);
        }

        // Mevcut satýrlarý güncelle
        foreach (var kv in rows)
            kv.Value.Refresh();
    }
}
