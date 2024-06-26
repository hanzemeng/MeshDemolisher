using System;

namespace Hanzzz.MeshDemolisher
{

public struct Int3
{
    public int int0;
    public int int1;
    public int int2;

    public Int3(int int0, int int1, int int2)
    {
        this.int0 = int0;
        this.int1 = int1;
        this.int2 = int2;
    }

    public int this[int index]
    {
        get => Getter(index);
    }

    public bool Contains(int int4)
    {
        return
            int0 == int4 || 
            int1 == int4 || 
            int2 == int4;
    }

    private int Getter(int index)
    {
        switch(index)
        {
            case 0:
                return int0;
            case 1:
                return int1;
            case 2:
                return int2;
            default:
                throw new Exception();
        }
    }
}

public struct Int4
{
    public int int0;
    public int int1;
    public int int2;
    public int int3;

    public Int4(int int0, int int1, int int2, int int3)
    {
        this.int0 = int0;
        this.int1 = int1;
        this.int2 = int2;
        this.int3 = int3;
    }

    public int this[int index]
    {
        get => Getter(index);
        set => Setter(index, value);
    }

    public bool Contains(int int4)
    {
        return
            int0 == int4 || 
            int1 == int4 || 
            int2 == int4 || 
            int3 == int4;
    }

    private int Getter(int index)
    {
        switch(index)
        {
            case 0:
                return int0;
            case 1:
                return int1;
            case 2:
                return int2;
            case 3:
                return int3;
            default:
                throw new Exception();
        }
    }

    private void Setter(int index, int value)
    {
        switch(index)
        {
            case 0:
                int0 = value;
                break;
            case 1:
                int1 = value;
                break;
            case 2:
                int2 = value;
                break;
            case 3:
                int3 = value;
                break;
            default:
                throw new Exception();
        }
    }
}

public struct Int5
{
    public int int0;
    public int int1;
    public int int2;
    public int int3;
    public int int4;

    public Int5(int int0, int int1, int int2, int int3, int int4)
    {
        this.int0 = int0;
        this.int1 = int1;
        this.int2 = int2;
        this.int3 = int3;
        this.int4 = int4;
    }

    public int this[int index]
    {
        get => Getter(index);
        set => Setter(index, value);
    }

    private int Getter(int index)
    {
        switch(index)
        {
            case 0:
                return int0;
            case 1:
                return int1;
            case 2:
                return int2;
            case 3:
                return int3;
            case 4:
                return int4;
            default:
                throw new Exception();
        }
    }

    private void Setter(int index, int value)
    {
        switch(index)
        {
            case 0:
                int0 = value;
                break;
            case 1:
                int1 = value;
                break;
            case 2:
                int2 = value;
                break;
            case 3:
                int3 = value;
                break;
            case 4:
                int4 = value;
                break;
            default:
                throw new Exception();
        }
    }
}


public struct Group4<T> where T : struct
{
    public T item0;
    public T item1;
    public T item2;
    public T item3;

    public Group4(T item0, T item1, T item2, T item3)
    {
        this.item0 = item0;
        this.item1 = item1;
        this.item2 = item2;
        this.item3 = item3;
    }

    public T this[int index]
    {
        get => Getter(index);
        set => Setter(index, value);
    }

    private T Getter(int index)
    {
        switch(index)
        {
            case 0:
                return item0;
            case 1:
                return item1;
            case 2:
                return item2;
            case 3:
                return item3;
            default:
                throw new Exception();
        }
    }

    private void Setter(int index, T value)
    {
        switch(index)
        {
            case 0:
                item0 = value;
                break;
            case 1:
                item1 = value;
                break;
            case 2:
                item2 = value;
                break;
            case 3:
                item3 = value;
                break;
            default:
                throw new Exception();
        }
    }
}

}
