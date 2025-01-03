using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DelaunayHelper
{
	private const float Margin = 3f;

	/// <summary> Generates a 'Supra/Super Triangle' which encapsulates all points held within set bounds </summary>
	public static Triangle GenerateSupraTriangle( PointBounds bounds )
	{
		float dMax = Mathf.Max(bounds.maxX - bounds.minX, bounds.maxY - bounds.minY) * Margin;
		float xCen = (bounds.minX + bounds.maxX) * 0.5f;
		float yCen = (bounds.minY + bounds.maxY) * 0.5f;

		///The float 0.866 is an arbitrary value determined for optimum supra triangle conditions.
		float x1 = xCen - 0.866f * dMax;		// 사각형 바운드를 포함할 수 있는 삼각형 바운드의 폭의 값을 계산하기 위한 것.
		float x2 = xCen + 0.866f * dMax;		// 왼쪽 오른쪽으로 벌려준다.
		float x3 = xCen;

		float y1 = yCen - 0.5f * dMax;			// 세로는 사각형 바운드를 가지고 해도 충분하다.
		float y2 = yCen - 0.5f * dMax;
		float y3 = yCen + dMax;

		Point pointA = new Point(x1, y1);
		Point pointB = new Point(x2, y2);
		Point pointC = new Point(x3, y3);

		return new Triangle( pointA, pointB, pointC );
	}

	/// <summary> Returns a set of bounds encolsing a point set </summary>
	public static PointBounds GetPointBounds( List<Point> points )
	{
		float minX = Mathf.Infinity;
		float minY = Mathf.Infinity;
		float maxX = Mathf.NegativeInfinity;
		float maxY = Mathf.NegativeInfinity;

		for ( int i = 0; i < points.Count; i++ )
		{
			Point p = points[i];
			if ( minX > p.x )
			{
				minX = p.x;
			}
			if ( minY > p.y )
			{
				minY = p.y;
			}
			if ( maxX < p.x )
			{
				maxX = p.x;
			}
			if ( maxY < p.y )
			{
				maxY = p.y;
			}
		}
		return new PointBounds( minX, minY, maxX, maxY );
	}


	///

	// ---------------------------------------------------------------------------------------------------
	// 트라이 앵글 만들기
	// ---------------------------------------------------------------------------------------------------


	// ---------------------------------------------------------------------------------------------------
	/// <summary> 
	/// Triangulates a set of points utilising the Bowyer Watson Delaunay technique 
	/// LSH : 20250102 목 : 
	/// 1. 이 단순한 로직이 전체를 기적을 만들어 낸다.
	/// 2. 이걸 처음보고 다시보고 이해 할때까지 거의 2년이 걸렸다. ( 2022년 가을쯤에 보고, 23년, 24년.... 시간만 흘러갔다.... 그리고 2025년 1월 02이 다시본다. )
	/// 3. 필요한 것
	///			1. 외접원, 내접원의 개념
	///			2. 또 그걸 코딩적으로 가장 최적화 해서 찾아내는 알고리즘
	///			3. 유니크한 엣지를 구하는 최적화 알고리즘.
	///			4. 
	///			
	/// 4. 개선안
	///			1. 각 포인트를 new Point( a , b , c )로 점을 찍는 것이 아니라.
	///			2. 인덱스화 하면 더 빠르게 할 수 있다.
	/// </summary>
	// ---------------------------------------------------------------------------------------------------
	public static List<Triangle> Delaun( List<Point> points )
	{
		///TODO - Plenty of optimizations for this algorithm to be implemented
		points = new List<Point>( points );

		//Create an empty triangle list
		List<Triangle> triangles = new List<Triangle>();

		//Generate supra triangle to ecompass all points and add it to the empty triangle list
		PointBounds bounds = GetPointBounds(points);
		Triangle supraTriangle = GenerateSupraTriangle(bounds);
		triangles.Add( supraTriangle );


		// [ 모든 점을 1번씩만 순회하면서 ]
		//Loop through points and carry out the triangulation
		for ( int pIndex = 0; pIndex < points.Count; pIndex++ )
		{
			Point p = points[pIndex];
			List<Triangle> badTriangles = new List<Triangle>();

			//Identify 'bad triangles'
			// LSH : 한점에 대해서 , 현재 생성된 모든 삼각형이, Bad Triangle 인지 먼저 체크한다.
			// LSH : 이렇게 되었을 때, 복잡한 구조라고 할지라도 이루프를 계속 돌면 마지막에 정리가 된다.
			for ( int triIndex = triangles.Count - 1; triIndex >= 0; triIndex-- )
			{
				Triangle triangle = triangles[triIndex];

				//A 'bad triangle' is defined as a triangle who's CircumCentre contains the current point
				float dist = Vector2.Distance(p.pos, triangle.circumCentre);
				if ( dist < triangle.circumRadius )
				{
					badTriangles.Add( triangle );
				}
			}

			Debug.Log( $"Bad Triangle Count = {badTriangles.Count}" );


			// LSh : 20250102 목 : 
			// 1. 모든 Bad Traingle에서 유니크한 엣지를 구한다.


			// [ ] : 아~~~ 하나의 점에 대해서 유니크한 엣지를 계속 다시 생성한다.
			// [ ] : 이부분도 중요하다.
			//Contruct a polygon from unique edges, i.e. ignoring duplicate edges inclusively
			List<Edge> polygon = new List<Edge>();



			// [ ] : 모든 [ 배드 삼각형 ]을 돌면서
			// [ ] : 모든 [ 배드 삼각형의 엣지 ]를 찾고
			// [ ] : 여기서 또, 다른 [ 배드 삼각형에 포함되지 않는 고유한 엣지 ]를 찾고
			// [ ] : 이 [ 고유한 엣지 ]들과, [ 점 P ] 를 연결하여, [ 일반 삼각형 ]을 만든다.

			// [ 모든 배드 삼각형을 순회하면서 ]
			for ( int i = 0; i < badTriangles.Count; i++ )
			{

				// [ 현재 배드 삼각형의 엣지 ]를 얻고
				Edge[] badTriangleEdges = badTriangles[i].GetEdges();

				// [ 현재 배드 삼각형의 엣지 ]를 순회하면서
				for ( int j = 0; j < badTriangleEdges.Length; j++ )
				{
					bool rejectEdge = false;

					// [ 모든 배드 삼각형에서 같은 엣지가 있는지 비교하고 ]
					for ( int k = 0; k < badTriangles.Count; k++ )
					{
						// < 이 부분이 알고리즘의 핵심이다. >
						// 1. [ 모든 베드 삼각형 ]에 대해서 [ 각각이 가지고 있는 ] [ 모든 엣지 ]를 구한다.
						// 2. [ 모든 엣지에 대해서 ]
						// 3. [ 현재 베드 삼각형이 소유한 엣지 ]만 [ 유니크한 엣지 ]로 기록한다.
						// 4. 이때, 이 유니크한 엣지는 계속해서 쌓인다.



						// 3. 순서가 중요하다.
						// LSH : 나 자신을 제외한, 다른 삼각형과 공유하고 있는 엣지만 뺀다. ( 공유한 엣지 제거 )
						// LSH : 이게 진짜 오묘하다.
						// 배드 삼각형이 자기 자신의 엣지를 에외하고, 다른 삼각형과 공유한 엣지에서만 그 엣지라인을 제거하는 로직이다.

						// 배드 삼각형의 엣지라인이 ===> 다른 배드 삼각형과 공유된 부분만 제거해 낸다.


						// 즉, [ 현재의 베드 삼각형 ]이 가지고 있는 [ 엣지 ]를 [ 다른 베드 삼각형]이 가지고 있다면,
						// 이, [ 엣지 ]를 
						// [ 유니크한 폴리곤 ]에 넣지 않는다.
						
						

						// [[[[[[ 이 부분이 핵심이다. ]]]]]] 
						// [[[[[[ 배드 삼각형의 중접된 엣지를 모두 제거하고 고유한 엣지만 남긴다. ]]]]]]]
						// [ 배드 삼각형들 중에 하나가, 해당 엣지를 가지고 있는데, 그게 자기 자신이 아니라면, 유니크한 엣지가 아니다. ]
						// [ 아 이분이 신비롭네. ]
						// [ 이게 무슨 의미이지 ? ] ===> 버리는 엣지를 찾는다.
						// ===> 아 ~~~ 공유된 엣지를 찾는다 ===> 

						// [ 모든 배드 삼각형의 엣지에 대해서 ] 그 엣지가 [ 다른 배드 삼각형 ]과 공유되었다면 [ 리젝 ] 한다.
						// ===> 배드 삼각형의 고유한 엣지들만 남는다.
						// ===> 이게 부정형으로 되어 있어서 ===> 이해가 어려웠었다.
						if ( badTriangles[ k ].ContainsEdge( badTriangleEdges[ j ] ) && k != i )
						{
							rejectEdge = true;
						}
					}

					// [ 버리는 엣지가 아닌 것만 모은다. ] == [ 
					// [ 배드 삼각형에서 찾아낸 유니크한 엣지만 모은다. ]
					if ( !rejectEdge )
					{
						polygon.Add( badTriangleEdges[ j ] );
					}
				}
			}


			// [ ] : LSH : 전체 삼각형에서 - [ 찾아낸 모든 베드 삼각형 ]을 버린다. ---> [ 베드 삼각형이 아닌 ] [ 일반 삼각형 ]만 남는다.

			// LSH : 그런데 이게 정말 딱 1번에 이렇게 되는가?
			// LSH : 모든 삼각형에 대해서 다 돌린다.

			// [ 배드 삼각형을 일반 삼각형 리스트에서 제거한다. - 처음 슈퍼삼각형은 첫 번째로 삭제된다. ] //Remove bad triangles from the triangulation
			for ( int i = badTriangles.Count - 1; i >= 0; i-- )
			{
				triangles.Remove( badTriangles[ i ] );
			}


			// [ 위에서 찾은 유니크한 엣지와 한점  P를 이용하여, 새로운 삼각형을 만든다. ] //Create new triangles
			for ( int i = 0; i < polygon.Count; i++ )
			{
				Edge edge = polygon[i];

				// [ ] : 하나의 점과, 유니크한 엣지( 끝점이 2개임 )을 연결하여 삼각형을 만든다.
				Point pointA = new Point(p.x, p.y);
				Point pointB = new Point(edge.vertexA);
				Point pointC = new Point(edge.vertexB);

				// 새로운 일반 삼각형 추가
				triangles.Add( new Triangle( pointA, pointB, pointC ) );
			}
		}




		// [ ] : LSH : 모든 삼각형이 만들어진 후, 최초에 만들었던, [ 슈퍼 삼각형 ]의 [ 점 ]을 포함하는 모든 [ 일반 삼각형 ]을 제거한다.


		//Finally, remove all triangles which share verticies with the supra triangle
		for ( int i = triangles.Count - 1; i >= 0; i-- )
		{
			Triangle triangle = triangles[i];
			for ( int j = 0; j < triangle.vertices.Length; j++ )
			{
				bool removeTriangle = false;
				Point vertex = triangle.vertices[j];
				for ( int s = 0; s < supraTriangle.vertices.Length; s++ )
				{
					if ( vertex.EqualsPoint( supraTriangle.vertices[ s ] ) )
					{
						removeTriangle = true;
						break;
					}
				}

				if ( removeTriangle )
				{
					triangles.RemoveAt( i );
					break;
				}
			}
		}

		Debug.Log( $"Finally" );

		return triangles;
	}

	public static Mesh CreateMeshFromTriangulation( List<Triangle> triangulation )
	{
		Mesh mesh = new Mesh();

		int vertexCount = triangulation.Count * 3;

		Vector3[] verticies = new Vector3[vertexCount];
		Vector2[] uvs = new Vector2[vertexCount];
		int[] triangles = new int[vertexCount];

		int vertexIndex = 0;
		int triangleIndex = 0;
		for ( int i = 0; i < triangulation.Count; i++ )
		{
			Triangle triangle = triangulation[i];

			verticies[ vertexIndex ] = new Vector3( triangle.vertA.x, triangle.vertA.y, 0f );
			verticies[ vertexIndex + 1 ] = new Vector3( triangle.vertB.x, triangle.vertB.y, 0f );
			verticies[ vertexIndex + 2 ] = new Vector3( triangle.vertC.x, triangle.vertC.y, 0f );

			uvs[ vertexIndex ] = triangle.vertA.pos;
			uvs[ vertexIndex + 1 ] = triangle.vertB.pos;
			uvs[ vertexIndex + 2 ] = triangle.vertC.pos;

			triangles[ triangleIndex ] = vertexIndex + 2;
			triangles[ triangleIndex + 1 ] = vertexIndex + 1;
			triangles[ triangleIndex + 2 ] = vertexIndex;

			vertexIndex += 3;
			triangleIndex += 3;
		}

		mesh.vertices = verticies;
		mesh.triangles = triangles;
		mesh.uv = uvs;
		return mesh;
	}
}