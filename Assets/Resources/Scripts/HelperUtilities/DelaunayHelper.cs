using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DelaunayHelper
{
    private const float Margin = 3f;

    /// <summary> Generates a 'Supra/Super Triangle' which encapsulates all points held within set bounds </summary>
    public static Triangle GenerateSupraTriangle(PointBounds bounds)
    {
        float dMax = Mathf.Max(bounds.maxX - bounds.minX, bounds.maxY - bounds.minY) * Margin;
        float xCen = (bounds.minX + bounds.maxX) * 0.5f;
        float yCen = (bounds.minY + bounds.maxY) * 0.5f;

        ///The float 0.866 is an arbitrary value determined for optimum supra triangle conditions.
        float x1 = xCen - 0.866f * dMax;
        float x2 = xCen + 0.866f * dMax;
        float x3 = xCen;

        float y1 = yCen - 0.5f * dMax;
        float y2 = yCen - 0.5f * dMax;
        float y3 = yCen + dMax;

        Point pointA = new Point(x1, y1);
        Point pointB = new Point(x2, y2);
        Point pointC = new Point(x3, y3);

        return new Triangle(pointA, pointB, pointC);
    }

    /// <summary> Returns a set of bounds encolsing a point set </summary>
    public static PointBounds GetPointBounds(List<Point> points)
    {
        float minX = Mathf.Infinity;
        float minY = Mathf.Infinity;
        float maxX = Mathf.NegativeInfinity;
        float maxY = Mathf.NegativeInfinity;

        for (int i = 0; i < points.Count; i++)
        {
            Point p = points[i];
            if (minX > p.x){
                minX = p.x;
            }
            if (minY > p.y){
                minY = p.y;
            }
            if (maxX < p.x){
                maxX = p.x;
            }
            if (maxY < p.y){
                maxY = p.y;
            }
        }
        return new PointBounds(minX, minY, maxX, maxY);
    }
	// ---------------------------------------------------------------------------------------------------
	// 트라이 앵글 만들기
	// ---------------------------------------------------------------------------------------------------

	/// <summary> Triangulates a set of points utilising the Bowyer Watson Delaunay technique </summary>
	public static List<Triangle> Delaun(List<Point> points)
    {
        ///TODO - Plenty of optimizations for this algorithm to be implemented
        points = new List<Point>(points);

        //Create an empty triangle list
        List<Triangle> triangles = new List<Triangle>();

        //Generate supra triangle to ecompass all points and add it to the empty triangle list
        PointBounds bounds = GetPointBounds(points);
        Triangle supraTriangle = GenerateSupraTriangle(bounds);
        triangles.Add(supraTriangle);

        //Loop through points and carry out the triangulation
        for (int pIndex = 0; pIndex < points.Count; pIndex++)
        {
            Point p = points[pIndex];
            List<Triangle> badTriangles = new List<Triangle>();

            //Identify 'bad triangles'
			// LSH : 한점에 대해서 현재 가지고 있는 모든 삼각형에 대해서 체크한다.
			// LSH : 이렇게 되었을 때, 복잡한 구조라고 할지라도 이루프를 계속 돌면 마지막에 정리가 된다.
            for (int triIndex = triangles.Count - 1; triIndex >= 0; triIndex--)
            {
                Triangle triangle = triangles[triIndex];

                //A 'bad triangle' is defined as a triangle who's CircumCentre contains the current point
                float dist = Vector2.Distance(p.pos, triangle.circumCentre);
                if (dist < triangle.circumRadius){
                    badTriangles.Add(triangle);
                }
            }


			Debug.Log( $"BadTriCtn = {badTriangles.Count}" );
			//Contruct a polygon from unique edges, i.e. ignoring duplicate edges inclusively
			List<Edge> polygon = new List<Edge>();
            for (int i = 0; i < badTriangles.Count; i++)
            {
				// LSH 
                //Triangle triangle = badTriangles[i];
                //Edge[] badTriangleEdges = triangle.GetEdges();
                Edge[] badTriangleEdges = badTriangles[i].GetEdges();

				// 나를 제외한 배드 삼각형에 있는 엣지만 뺀다.
                for (int j = 0; j < badTriangleEdges.Length; j++)
                {
                    bool rejectEdge = false;
                    for (int t = 0; t < badTriangles.Count; t++)
                    {
						// LSH : 나 자신을 제외한, 다른 삼각형과 공유하고 있는 엣지만 뺀다. ( 공유한 엣지 제거 )
						// LSH : 이게 진짜 오묘하다.
						// 배드 삼각형이 자기 자신의 엣지를 에외하고, 다른 삼각형과 공유한 엣지에서만 그 엣지라인을 제거하는 로직이다.

						// 이게 이 코딩의 핵심라인이다.
						// 배드 삼각형의 엣지라인이 ===> 다른 배드 삼각형과 공유된 부분만 제거해 낸다.
						if ( t != i && badTriangles[ t ].ContainsEdge( badTriangleEdges[ j ] ) )
						{
							rejectEdge = true;
                        }
                    }

					if ( !rejectEdge )
					{
						polygon.Add( badTriangleEdges[ j ] );
					}
				}
			}

			// LSH : 그런데 이게 정말 딱 1번에 이렇게 되는가?
			// LSH : 모든 삼각형에 대해서 다 돌린다.
			//Remove bad triangles from the triangulation
			for ( int i = badTriangles.Count - 1; i >= 0; i-- )
			{
				triangles.Remove( badTriangles[ i ] );
			}

			//Create new triangles
			for ( int i = 0; i < polygon.Count; i++)
            {
                Edge edge = polygon[i];
                Point pointA = new Point(p.x, p.y);
                Point pointB = new Point(edge.vertexA);
                Point pointC = new Point(edge.vertexB);
                triangles.Add(new Triangle(pointA, pointB, pointC));
            }
        }

        //Finally, remove all triangles which share verticies with the supra triangle
        for (int i = triangles.Count - 1; i >= 0; i--)
        {
            Triangle triangle = triangles[i];
            for (int j = 0; j < triangle.vertices.Length; j++)
            {
                bool removeTriangle = false;
                Point vertex = triangle.vertices[j];
                for (int s = 0; s < supraTriangle.vertices.Length; s++)
                {
                    if (vertex.EqualsPoint(supraTriangle.vertices[s]))
                    {
                        removeTriangle = true;
                        break;
                    }
                }

                if (removeTriangle)
                {
                    triangles.RemoveAt(i);
                    break;
                }
            }
        }

		Debug.Log( $"Finally" );

		return triangles;
    }

    public static Mesh CreateMeshFromTriangulation(List<Triangle> triangulation)
    {
        Mesh mesh = new Mesh();

        int vertexCount = triangulation.Count * 3;

        Vector3[] verticies = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        int[] triangles = new int[vertexCount];

        int vertexIndex = 0;
        int triangleIndex = 0;
        for (int i = 0; i < triangulation.Count; i++)
        {
            Triangle triangle = triangulation[i];

            verticies[vertexIndex] = new Vector3(triangle.vertA.x, triangle.vertA.y, 0f);
            verticies[vertexIndex + 1] = new Vector3(triangle.vertB.x, triangle.vertB.y, 0f);
            verticies[vertexIndex + 2] = new Vector3(triangle.vertC.x, triangle.vertC.y, 0f);

            uvs[vertexIndex] = triangle.vertA.pos;
            uvs[vertexIndex + 1] = triangle.vertB.pos;
            uvs[vertexIndex + 2] = triangle.vertC.pos;

            triangles[triangleIndex] = vertexIndex + 2;
            triangles[triangleIndex + 1] = vertexIndex + 1;
            triangles[triangleIndex + 2] = vertexIndex;

            vertexIndex += 3;
            triangleIndex += 3;
        }

        mesh.vertices = verticies;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        return mesh;
    }
}