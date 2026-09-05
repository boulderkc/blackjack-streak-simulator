window.getBrowserTimeZone = () => {
    return Intl.DateTimeFormat().resolvedOptions().timeZone;
};
