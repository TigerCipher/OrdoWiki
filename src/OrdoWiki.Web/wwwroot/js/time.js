window.ordoTime = {
    format: (utcIso, mode) => {
        const d = new Date(utcIso);
        switch(mode) {
            case "date": return d.toLocaleDateString();
            case "time": return d.toLocaleTimeString();
            case "relative": return new Intl.RelativeTimeFormat(undefined, { numeric: "auto" })
                .format(Math.round((d - new Date()) / 60000), "minute");
            default: return d.toLocaleString();
        }
    }
};