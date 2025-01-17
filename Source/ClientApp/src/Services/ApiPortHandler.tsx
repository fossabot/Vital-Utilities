import { invoke } from "@tauri-apps/api";
import React, { useEffect } from "react";
import { useDispatch, useSelector } from "react-redux";
import { LaunchSettings } from "../Dtos/Dto";
import { updateApiPortAction } from "../Redux/actions/appActions";
import { State } from "../Redux/States";

export const ApiPortHandler: React.FunctionComponent = () => {
    const dispatch = useDispatch();
    const appPort = useSelector<State, number | undefined>(state => state.appState.vitalServicePort);
    useEffect(() => {
        invoke<LaunchSettings>("get_vital_service_ports")
            .then(response => {
                if (response.vitalServiceHttpsPort !== appPort) dispatch(updateApiPortAction(response.vitalServiceHttpsPort));
            })
            .catch(error => console.error(error));
    });

    return <></>;
};
