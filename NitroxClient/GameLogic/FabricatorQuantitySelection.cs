using System;

namespace NitroxClient.GameLogic;

internal sealed class FabricatorQuantitySelection
{
    public int Quantity { get; private set; } = 1;
    public int Maximum { get; private set; }
    public bool CanDecrement => Quantity > 1;
    public bool CanIncrement => Maximum > 0 && Quantity < Maximum;
    public bool CanFabricate => Maximum > 0 && Quantity <= Maximum;

    public void Reset(int maximum)
    {
        Quantity = 1;
        SetMaximum(maximum);
    }

    public void SetMaximum(int maximum)
    {
        Maximum = Math.Max(0, maximum);
        Quantity = Maximum > 0 ? Math.Min(Quantity, Maximum) : 1;
    }

    public void Increment()
    {
        if (CanIncrement)
        {
            Quantity++;
        }
    }

    public void Decrement()
    {
        if (CanDecrement)
        {
            Quantity--;
        }
    }
}
