function formatDate(string) {
    var now = new Date(Date.now());
    var date = new Date(Date.parse(string));

    if (now.getDay() == date.getDay() && now.getMonth() == date.getMonth() && now.getYear() == date.getYear())
        return date.getHours() + " : " + date.getMinutes();

    if (now.getMonth() == date.getMonth() && now.getYear() == date.getYear()) return date.getDay() + " / " + now.getMonth();

    return date.getYear().toString();
}


function newTestDrive(id) {
    $("#newTestDriveContact").val(id);
    $('#modalNewTestDrive').modal();
}


function newRequest(id) {
    $("#newContactRequest").val(id);
    $('#modalNewRequest').modal();
}

function patologyName(p)
{
    switch (p)
    {
        case 128: return "Paraplegia";
        case 64: return "Tetraplegia";
        case 32: return "Amputee";
        case 16: return "SplitSpine";
        case 8: return "MultipleSclerosis";
        case 4: return "Polio";
        case 2: return "None";
        case 1: return "Other";
        case 0: return "Unknow";
    }
}