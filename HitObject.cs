namespace osuproyecto;

public class HitObject
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Time { get; set; }
    public int ObjectType { get; set; }
    public bool IsHitCircle
    {
        get
        {
            if ((ObjectType & 1) != 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
    public bool IsSlider
    {
        get
        {
            if ((ObjectType & 2) != 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
    public bool StarstNewCombo
    {
        get
        {
            if ((ObjectType & 4) != 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
    public bool IsSpinner
    {
        get
        {
            if ((ObjectType & 8) != 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
    public int HitSound { get; set; }
    public HitSample HitSample {get; set;} = new HitSample();
    public bool IsNormalOnly
    {
        get
        {
            if (!HasWhistle && !HasFinish && !HasClap)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
    public bool HasWhistle
    {
        get
        {
            if ((HitSound & 2) != 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
    public bool HasFinish
    {
        get
        {
            if ((HitSound & 4) != 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
    public bool HasClap
    {
        get
        {
            if ((HitSound & 8) != 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
    public int? EndTime {get; set;}
}
