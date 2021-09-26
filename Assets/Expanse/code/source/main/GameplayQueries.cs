using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Expanse {

/**
 * @brief: static singleton class for submitting queries related to
 * gameplay logic. This includes density sampling, visibility sampling, 
 * etc.
 * 
 * Queries are resolved as the main camera is rendered.
 * */
public static class GameplayQueries {

    [GenerateHLSL(needAccessors=false)]
    public struct QueryInfo {
        // Inputs.
        public Vector3 startWS;
        public Vector3 endWS;
        // Outputs.
        public float density;
        public float visibility;
    };

    /* Sets query to execute when clouds are rendered. Calls callback upon
     * successful fulfillment of the query. */
    public static void Dispatch(QueryInfo query, Action<QueryInfo> callback) {
        s_waitingQueries.Add(query);
        s_waitingCallbacks.Add(callback);
    }


    private static List<QueryInfo> s_waitingQueries = new List<QueryInfo>();
    private static List<Action<QueryInfo>> s_waitingCallbacks = new List<Action<QueryInfo>>();
    private static List<QueryInfo> s_inProgressQueries = new List<QueryInfo>();
    private static List<Action<QueryInfo>> s_inProgressCallbacks = new List<Action<QueryInfo>>();
    private static List<QueryInfo> s_processedQueries = new List<QueryInfo>();
    private static List<Action<QueryInfo>> s_processedCallbacks = new List<Action<QueryInfo>>();
    
    public static void BeginProcessing() {
        s_inProgressQueries.AddRange(s_waitingQueries);
        s_inProgressCallbacks.AddRange(s_waitingCallbacks);
        s_waitingQueries.Clear();
        s_waitingCallbacks.Clear();
    }

    public static void EndProcessing() {
        s_processedQueries.AddRange(s_inProgressQueries);
        s_processedCallbacks.AddRange(s_inProgressCallbacks);
        s_inProgressQueries.Clear();
        s_inProgressCallbacks.Clear();
    }

    public static List<QueryInfo> GetInProgressQueries() {
        return s_inProgressQueries;
    }

    public static List<Action<QueryInfo>> GetInProgressCallbacks() {
        return s_inProgressCallbacks;
    }

    public static List<Action<QueryInfo>> GetProcessedCallbacks() {
        return s_processedCallbacks;
    }

    public static void ClearProcessed(int toClear) {
        s_processedQueries.RemoveRange(0, toClear);
        s_processedCallbacks.RemoveRange(0, toClear);
    }

    public static void ClearProcessed() {
        s_processedQueries.Clear();
        s_processedCallbacks.Clear();
    }
}

} // namespace Expanse