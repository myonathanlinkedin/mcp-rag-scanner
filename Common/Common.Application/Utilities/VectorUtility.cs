using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class VectorUtility
{
    // Helper method to compute cosine similarity between two vectors
    public static float ComputeCosineSimilarity(float[] vector1, float[] vector2)
    {
        var dotProduct = 0f;
        var magnitude1 = 0f;
        var magnitude2 = 0f;

        for (int i = 0; i < vector1.Length; i++)
        {
            dotProduct += vector1[i] * vector2[i];
            magnitude1 += vector1[i] * vector1[i];
            magnitude2 += vector2[i] * vector2[i];
        }

        magnitude1 = MathF.Sqrt(magnitude1);
        magnitude2 = MathF.Sqrt(magnitude2);

        if (magnitude1 == 0 || magnitude2 == 0)
            return 0f;  // Prevent division by zero

        return dotProduct / (magnitude1 * magnitude2);
    }
}
