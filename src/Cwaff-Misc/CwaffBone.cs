namespace CwaffingTheGungy;

/// <summary>Class / interface for managing bones for various tile sprites and meshes.</summary>
public class CwaffBone
{
  private static readonly LinkedList<CwaffBone> _BonePool = new();
  private static int _BonesCreated = 0;

  public Vector2 pos;
  public Vector2 normal;

  internal static LinkedListNode<CwaffBone> Rent(Vector2 pos)
  {
    if (_BonePool.Count == 0)
      _BonePool.AddLast(new CwaffBone());

    LinkedListNode<CwaffBone> node = _BonePool.Last;
    _BonePool.RemoveLast();

    CwaffBone bone   = node.Value;
    bone.pos    = pos;
    bone.normal = default;

    return node;
  }

  internal static void Return(LinkedListNode<CwaffBone> bone)
  {
    _BonePool.AddLast(bone);
    // System.Console.WriteLine($"returned {_BonePool.Count}/{_BonesCreated} bones");
  }

  internal static void ReturnAll(ref LinkedList<CwaffBone> bones)
  {
    if (bones == null)
      return;
    while (bones.Count > 0)
    {
      LinkedListNode<CwaffBone> bone = bones.Last;
      bones.RemoveLast();
      _BonePool.AddLast(bone);
    }
    // System.Console.WriteLine($"returned {_BonePool.Count}/{_BonesCreated} bones");
  }

  private CwaffBone() // can only be created by Rent
  {
    ++_BonesCreated;
  }
}
