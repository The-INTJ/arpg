using System;
using System.Collections.Generic;
using Godot;

namespace ARPG;

public partial class CaveChest : Area3D
{
    private const double OpenAnimationFallbackDuration = 0.38;
    private static readonly string[] OpenAnimationTokens = { "open", "lid", "004" };

    private InventoryItem _item;
    private PlayerController _nearbyPlayer;
    private Node _chestModelRoot;
    private OmniLight3D _glowLight;
    private AnimationPlayer _animationPlayer;
    private string _openAnimationName;
    private Node3D _lidNode;
    private Vector3 _closedLidPosition;
    private Vector3 _closedLidRotation;
    private Label3D _nameLabel;
    private Label3D _promptLabel;
    private bool _opened;

    [Signal]
    public delegate void OpenedEventHandler(string itemName);

    public void Init(InventoryItem item)
    {
        _item = item;
        _chestModelRoot = GetNode<Node>("VisualRoot/ChestModelRoot");
        _glowLight = GetNodeOrNull<OmniLight3D>("VisualRoot/ChestGlow");
        ConfigureVisuals();

        _nameLabel = GetNode<Label3D>("NameLabel");
        _nameLabel.Text = "Cave Cache";
        _nameLabel.Modulate = Palette.TextLight;
        _nameLabel.OutlineModulate = Palette.BgDark;
        _nameLabel.Visible = false;

        _promptLabel = GetNode<Label3D>("PromptLabel");
        _promptLabel.Text = item.Name;
        _promptLabel.Modulate = item.DisplayColor;
        _promptLabel.OutlineModulate = Palette.BgDark;
        _promptLabel.Visible = false;

        Monitoring = true;
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    public override void _Process(double delta)
    {
        if (_opened || _nearbyPlayer == null || !_nearbyPlayer.IsGrounded)
            return;

        OpenChest();
    }

    private void OnBodyEntered(Node3D body)
    {
        if (_opened || body is not PlayerController player)
            return;

        _nearbyPlayer = player;
        _nameLabel.Visible = true;
        _promptLabel.Visible = true;
    }

    private void OnBodyExited(Node3D body)
    {
        if (body is not PlayerController)
            return;

        _nearbyPlayer = null;
        _nameLabel.Visible = false;
        _promptLabel.Visible = false;
    }

    private void OpenChest()
    {
        if (_opened || _item == null)
            return;

        _opened = true;
        Monitoring = false;
        _nameLabel.Visible = false;
        _promptLabel.Visible = false;

        double dropDelay = PlayOpenAnimation();
        FinishOpeningAsync(dropDelay);
    }

    private void ConfigureVisuals()
    {
        foreach (var mesh in EnumerateMeshInstances(_chestModelRoot))
            mesh.MaterialOverride = UsesWoodMaterial(mesh) ? WorldMaterials.GetChestWoodMaterial() : WorldMaterials.GetChestMetalMaterial();

        if (_glowLight != null)
        {
            _glowLight.LightColor = Palette.ChestMetal.Lightened(0.08f);
            _glowLight.LightEnergy = 0.38f;
            _glowLight.OmniRange = 2.4f;
            _glowLight.ShadowEnabled = false;
        }

        _animationPlayer = FindAnimationPlayer(_chestModelRoot);
        _openAnimationName = FindOpenAnimationName(_animationPlayer);
        _lidNode = FindLidNode(_chestModelRoot);

        if (_lidNode != null)
        {
            _closedLidPosition = _lidNode.Position;
            _closedLidRotation = _lidNode.Rotation;
        }

        ResetToClosedPose();
    }

    private double PlayOpenAnimation()
    {
        if (_animationPlayer != null && !string.IsNullOrEmpty(_openAnimationName))
        {
            var animation = _animationPlayer.GetAnimation(_openAnimationName);
            _animationPlayer.Play(_openAnimationName);
            return animation?.Length ?? OpenAnimationFallbackDuration;
        }

        if (_lidNode == null)
            return 0.0;

        var tween = CreateTween();
        tween.TweenProperty(_lidNode, "position", _closedLidPosition + new Vector3(0.40f, 0.47f, 0.0f), OpenAnimationFallbackDuration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        tween.SetParallel(true);
        tween.TweenProperty(_lidNode, "rotation", _closedLidRotation + new Vector3(0.0f, 1.34f, 0.0f), OpenAnimationFallbackDuration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        return OpenAnimationFallbackDuration;
    }

    private async void FinishOpeningAsync(double dropDelay)
    {
        if (dropDelay > 0.0)
            await ToSignal(GetTree().CreateTimer((float)dropDelay), SceneTreeTimer.SignalName.Timeout);

        if (!IsInsideTree())
            return;

        var pickup = GD.Load<PackedScene>(Scenes.ItemPickup).Instantiate<ItemPickup>();
        pickup.GlobalPosition = GlobalPosition + new Vector3(0.38f, 0.16f, 0.18f);
        pickup.Init(_item);
        GetParent<Node3D>().AddChild(pickup);

        EmitSignal(SignalName.Opened, _item.Name);
    }

    private void ResetToClosedPose()
    {
        if (_lidNode != null)
        {
            _lidNode.Position = _closedLidPosition;
            _lidNode.Rotation = _closedLidRotation;
        }

        if (_animationPlayer == null || string.IsNullOrEmpty(_openAnimationName))
            return;

        _animationPlayer.Play(_openAnimationName);
        _animationPlayer.Seek(0.0, true);
        _animationPlayer.Stop();
    }

    private static bool UsesWoodMaterial(MeshInstance3D mesh)
    {
        var aabb = mesh.GetAabb();
        float longestSide = Math.Max(aabb.Size.X, Math.Max(aabb.Size.Y, aabb.Size.Z));
        string meshName = mesh.Name.ToString();
        return longestSide >= 0.9f || meshName.Contains("Cube", StringComparison.OrdinalIgnoreCase);
    }

    private static AnimationPlayer FindAnimationPlayer(Node root)
    {
        if (root is AnimationPlayer animationPlayer)
            return animationPlayer;

        foreach (Node child in root.GetChildren())
        {
            var nested = FindAnimationPlayer(child);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private static string FindOpenAnimationName(AnimationPlayer animationPlayer)
    {
        if (animationPlayer == null)
            return null;

        var names = animationPlayer.GetAnimationList();
        for (int i = 0; i < names.Length; i++)
        {
            string name = names[i];
            for (int tokenIndex = 0; tokenIndex < OpenAnimationTokens.Length; tokenIndex++)
            {
                if (name.Contains(OpenAnimationTokens[tokenIndex], StringComparison.OrdinalIgnoreCase))
                    return name;
            }
        }

        return names.Length > 0 ? names[names.Length - 1] : null;
    }

    private static Node3D FindLidNode(Node root)
    {
        foreach (Node3D node in EnumerateNode3Ds(root))
        {
            string nodeName = node.Name.ToString();
            if (nodeName.Contains("004", StringComparison.OrdinalIgnoreCase) ||
                nodeName.Contains("lid", StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }
        }

        return null;
    }

    private static IEnumerable<MeshInstance3D> EnumerateMeshInstances(Node root)
    {
        if (root is MeshInstance3D mesh)
            yield return mesh;

        foreach (Node child in root.GetChildren())
        {
            foreach (var nestedMesh in EnumerateMeshInstances(child))
                yield return nestedMesh;
        }
    }

    private static IEnumerable<Node3D> EnumerateNode3Ds(Node root)
    {
        if (root is Node3D node3D)
            yield return node3D;

        foreach (Node child in root.GetChildren())
        {
            foreach (var nestedNode in EnumerateNode3Ds(child))
                yield return nestedNode;
        }
    }
}
