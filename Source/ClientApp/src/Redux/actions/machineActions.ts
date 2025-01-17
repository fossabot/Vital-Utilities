import { AnyAction } from "@reduxjs/toolkit";
import axios from "axios";
import { GetMachineDynamicDataResponse, GetMachineRelativeTimeSeriesRequest, GetMachineStaticDataResponse, TimeSeriesMachineMetricsResponse } from "../../Dtos/Dto";

export type MachineActionTypes = UpdateMachineDynamicDataAction | UpdateMachineStaticDataAction | UpdateMachineTimeSeriesDataAction;

const UPDATE_MACHINE_DYNAMIC_DATA = "UPDATE_MACHINE_DYNAMIC_DATA";
const UPDATE_MACHINE_STATIC_DATA = "UPDATE_MACHINE_STATIC_DATA";
const UPDATE_MACHINE_TIMESERIES_DATA = "UPDATE_MACHINE_TIMESERIES_DATA";
interface UpdateMachineDynamicDataAction {
    type: typeof UPDATE_MACHINE_DYNAMIC_DATA;
    message: GetMachineDynamicDataResponse;
}

interface UpdateMachineStaticDataAction {
    type: typeof UPDATE_MACHINE_STATIC_DATA;
    message: GetMachineStaticDataResponse;
}
interface UpdateMachineTimeSeriesDataAction {
    type: typeof UPDATE_MACHINE_TIMESERIES_DATA;
    message: TimeSeriesMachineMetricsResponse;
}

function recieveMachineTimeSeriesData(message: TimeSeriesMachineMetricsResponse) {
    return { type: UPDATE_MACHINE_TIMESERIES_DATA, message: message };
}

function recieveMachineDynamicData(message: GetMachineDynamicDataResponse) {
    return { type: UPDATE_MACHINE_DYNAMIC_DATA, message: message };
}
function recieveMachineStaticData(message: GetMachineStaticDataResponse) {
    return { type: UPDATE_MACHINE_STATIC_DATA, message: message };
}
async function sendGetMachineStaticRequest() {
    return axios
        .get<GetMachineStaticDataResponse>("api/system/static")
        .then(response => response.data)
        .catch(e => {
            console.error(e);
            return Promise.reject(e);
        });
}
function sendGetMachineDynamicRequest() {
    return axios
        .get<GetMachineDynamicDataResponse>("api/system/dynamic")
        .then(response => response.data)
        .catch(e => {
            console.error(e);
            return Promise.reject(e);
        });
}

function sendGetMachineTimeSeriesRequest(message: GetMachineRelativeTimeSeriesRequest) {
    return axios
        .post<TimeSeriesMachineMetricsResponse>("api/system/timeseries/relative", message, {
            headers: {
                "Content-Type": "application/json"
            }
        })
        .then(response => response.data)
        .catch(e => {
            console.error(e);
            return Promise.reject(e);
        });
}
export function fetchMachineTimeSeriesDataAction(message: GetMachineRelativeTimeSeriesRequest): AnyAction {
    //@ts-ignore
    return function (dispatch) {
        return sendGetMachineTimeSeriesRequest(message)
            .then(result => dispatch(recieveMachineTimeSeriesData(result)))
            .catch(e => console.error(e));
    };
}
export function fetchMachineStaticDataAction(): AnyAction {
    //@ts-ignore
    return function (dispatch) {
        return sendGetMachineStaticRequest()
            .then(result => dispatch(recieveMachineStaticData(result)))
            .catch(e => console.error(e));
    };
}

export function fetchMachineDynamicDataAction() {
    //@ts-ignore
    return function (dispatch) {
        return sendGetMachineDynamicRequest()
            .then(result => dispatch(recieveMachineDynamicData(result)))
            .catch(e => console.error(e));
    };
}
export function recieveMachineDynamicDataAction(entities: GetMachineDynamicDataResponse) {
    //@ts-ignore
    return function (dispatch) {
        dispatch(recieveMachineDynamicData(entities));
    };
}
