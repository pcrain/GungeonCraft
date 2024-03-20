namespace CwaffingTheGungy;

public class ChestScanner : PlayerItem
{
    public static string ItemName         = "Chest Scanner";
    public static string ShortDescription = "Try Before You Buy";
    public static string LongDescription  = "TBD";
    public static string Lore             = "The tricky thing about most chests in the Gungeon is that their contents are seemingly not determined until they are opened, making scanning them a largely fruitless endeavor. Similar to an over-eager child on Christmas Eve, this handy little device operates by shaking chests at a sub-atomic level, tricking them into thinking they've been opened before using half-century old x-ray technologies to determine their contents.";

    private PlayerController _owner = null;
    private Chest _nearestChest     = null;

    public static void Init()
    {
        PlayerItem item = Lazy.SetupActive<ChestScanner>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.B;
        ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Timed, 1);
        item.consumable   = false;
        item.CanBeDropped = true;
    }

    public override void Pickup(PlayerController player)
    {
        this._owner = player;
        base.Pickup(player);
    }

    public override void OnPreDrop(PlayerController player)
    {
        this._owner = null;
        base.OnPreDrop(player);
    }

    public override bool CanBeUsed(PlayerController user)
    {
        if (!base.CanBeUsed(user))
            return false;

        this._nearestChest = null;
        foreach (Chest chest in StaticReferenceManager.AllChests.EmptyIfNull())
        {
            if (!chest)
                continue;
            if (chest.IsOpen || chest.IsBroken)
                continue;
            // if (chest.IsMimic)
            //     continue;
            if (chest.GetAbsoluteParentRoom() != user.CurrentRoom)
                continue;
            if (chest.gameObject.GetComponent<ScannedChest>())
                continue;
            this._nearestChest = chest;
            return true;
        }
        return false;
    }

    public override void DoEffect(PlayerController user)
    {
        if (!this._nearestChest)
            return;

        this._nearestChest.DetermineContents(user, 0);
        this._nearestChest.gameObject.AddComponent<ScannedChest>();
    }
}

public class ScannedChest : MonoBehaviour
{
    private List<GameObject> _pickups = new();
    private List<Vector2>    _offsets = new();
    private Vector2 _basePos          = Vector3.zero;
    private Chest _chest              = null;

    private IEnumerator Start()
    {
        this._basePos    = base.GetComponent<tk2dSprite>().WorldTopCenter + new Vector2(0f, 1f);
        this._chest      = base.GetComponent<Chest>();
        RoomHandler room = this._chest.GetAbsoluteParentRoom();

        room.DeregisterInteractable(this._chest as IPlayerInteractable);
        tk2dSprite sprite          = this._chest.GetComponent<tk2dSprite>();
        bool oldOverrideMaterial   = sprite.usesOverrideMaterial;
        Shader oldShader           = sprite.renderer.material.shader;
        sprite.MakeHolographic(green: false);
        base.gameObject.Play("gun_synthesizer_activate_sound");
        yield return new WaitForSeconds(0.75f);
        sprite.usesOverrideMaterial     = oldOverrideMaterial;
        sprite.renderer.material.shader = oldShader;
        room.RegisterInteractable(this._chest as IPlayerInteractable);

        int i = 0;
        float cumulativeWidth = 0f;
        foreach (PickupObject p in this._chest.contents.EmptyIfNull())
        {
            GameObject g = new GameObject();
            this._pickups.Add(g);
            tk2dSprite s = g.AddComponent<tk2dSprite>();
            s.SetSprite(p.sprite.collection, p.sprite.spriteId);
            s.SetAlpha(0.5f);
            this._offsets.Add(new Vector2(cumulativeWidth, 0f));
            cumulativeWidth += 0.25f + s.GetCurrentSpriteDef().boundsDataExtents.x;
            ++i;
        }
        cumulativeWidth *= 0.5f;
        for (int j = 0; j < this._offsets.Count; ++j)
            this._offsets[j] = new Vector2(this._offsets[j].x - cumulativeWidth, 0f);
    }

    private void Update()
    {
        if (!this._chest || this._chest.IsOpen || this._chest.IsBroken)
        {
            UnityEngine.Object.Destroy(this);
            return;
        }

        int i = 0;
        foreach (GameObject g in this._pickups)
            g.GetComponent<tk2dSprite>().PlaceAtPositionByAnchor(
                this._offsets[i++] + this._basePos.HoverAt(amplitude: 0.15f, frequency: 4f), Anchor.LowerLeft);
    }

    private void OnDestroy()
    {
        this._pickups.SafeDestroyAll();
    }
}
