using System;
using UnityEngine;

namespace Hanzzz.MeshDemolisher
{

public struct FloatStruct
{
    public Type type;
    public int dimension;

    public float float0;
    public float float1;
    public float float2;
    public float float3;

    //public FloatStruct(int _)
    //{
    //    type = null;
    //    dimension = -1;
    //    float0 = float.NaN;
    //    float1 = float.NaN;
    //    float2 = float.NaN;
    //    float3 = float.NaN;
    //}

    public FloatStruct(Vector2 vector2)
    {
        type = typeof(Vector2);
        dimension = 2;

        float0 = vector2.x;
        float1 = vector2.y;
        float2 = float.NaN;
        float3 = float.NaN;
    }

    public FloatStruct(Vector3 vector3)
    {
        type = typeof(Vector3);
        dimension = 3;

        float0 = vector3.x;
        float1 = vector3.y;
        float2 = vector3.z;
        float3 = float.NaN;
    }

    public FloatStruct(Vector4 vector4)
    {
        type = typeof(Vector4);
        dimension = 4;

        float0 = vector4.x;
        float1 = vector4.y;
        float2 = vector4.z;
        float3 = vector4.w;
    }

    public FloatStruct(Color color)
    {
        type = typeof(Color);
        dimension = 4;

        float0 = color.r;
        float1 = color.g;
        float2 = color.b;
        float3 = color.a;
    }

    public Vector2 ToVector2()
    {
        return new Vector2(float0,float1);
    }
    public Vector3 ToVector3()
    {
        return new Vector3(float0,float1,float2);
    }
    public Vector4 ToVector4()
    {
        return new Vector4(float0,float1,float2,float3);
    }
    public Color ToColor()
    {
        return new Color(float0,float1,float2,float3);
    }

    public (object, Type) ToOriginalType()
    {
        object res;
        if(typeof(Vector2) == type)
        {
            res = new Vector2(float0,float1);
        }
        else if(typeof(Vector3) == type)
        {
            res = new Vector3(float0,float1,float2);
        }
        else if(typeof(Vector4) == type)
        {
            res = new Vector4(float0,float1,float2,float3);
        }
        else if(typeof(Color) == type)
        {
            res = new Color(float0,float1,float2,float3);
        }
        else
        {
            throw new Exception();
        }
        return (res, type);
    }

    public FloatStruct DefaultValue()
    {
        FloatStruct res = new FloatStruct();

        res.type = this.type;
        res.dimension = this.dimension;

        for(int i=0; i<this.dimension; i++)
        {
            res[i] = 0f;
        }
        for(int i=this.dimension; i<4; i++)
        {
            res[i] = float.NaN;
        }
        return res;
    }

    public float this[int index]
    {
        get => Getter(index);
        set => Setter(index, value);
    }

    private float Getter(int index)
    {
        switch(index)
        {
            case 0:
                return float0;
            case 1:
                return float1;
            case 2:
                return float2;
            case 3:
                return float3;
            default:
                throw new Exception();
        }
    }
    private void Setter(int index, float value)
    {
        switch(index)
        {
            case 0:
                float0 = value;
                break;
            case 1:
                float1 = value;
                break;
            case 2:
                float2 = value;
                break;
            case 3:
                float3 = value;
                break;
            default:
                throw new Exception();
        }
    }

    public static FloatStruct operator+(FloatStruct a, FloatStruct b)
    {
        if(a.type != b.type)
        {
            throw new Exception();
        }

        FloatStruct res = new FloatStruct();

        res.type = a.type;
        res.dimension = a.dimension;
        for(int i=0; i<a.dimension; i++)
        {
            res[i] = a[i]+b[i];
        }
        for(int i=a.dimension; i<4; i++)
        {
            res[i] = float.NaN;
        }

        return res;
    }

    public static FloatStruct operator/(FloatStruct a, float b)
    {
        FloatStruct res = new FloatStruct();

        res.type = a.type;
        res.dimension = a.dimension;
        for(int i=0; i<a.dimension; i++)
        {
            res[i] = a[i] / b;
        }
        for(int i=a.dimension; i<4; i++)
        {
            res[i] = float.NaN;
        }

        return res;
    }

    public static FloatStruct operator*(float b, FloatStruct a)
    {
        return a*b;
    } 
    public static FloatStruct operator*(FloatStruct a, float b)
    {
        FloatStruct res = new FloatStruct();

        res.type = a.type;
        res.dimension = a.dimension;
        for(int i=0; i<a.dimension; i++)
        {
            res[i] = a[i] * b;
        }
        for(int i=a.dimension; i<4; i++)
        {
            res[i] = float.NaN;
        }

        return res;
    }
}

}
