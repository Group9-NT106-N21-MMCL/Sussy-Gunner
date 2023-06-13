import { moduleName } from "./match_handler";

export function rpcCreateMatch(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, payload: string): string {
    const query: { matchName: string } = JSON.parse(payload);
    const matchId = nk.matchCreate(moduleName, {});
    nk.localcachePut(query.matchName, matchId);
    return JSON.stringify({ matchId });
}

export function rpcFindMatch(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, payload: string): string {
    try {
        const query: { matchName: string } = JSON.parse(payload);
        const matchId: string = nk.localcacheGet(query.matchName);
        return JSON.stringify({ matchId });
    } catch (e) {
        return JSON.stringify({});
    }
}
