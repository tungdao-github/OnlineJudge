let count = 1;

document.addEventListener("DOMContentLoaded", async () => {
   const problemId = new URLSearchParams(location.search).get("problemId");
   const userId = localStorage.getItem("userId");
    let response = await fetch('http://localhost:5024/api/Submissions/historyproblemuser?problemId=' + problemId + '&userId=' + userId);
    var result = await response.json();
    // historyproblemuser.for(element => {
    //     document.createElement("p").innerHTML = element.code
    // });
    console.log(result);
    console.log(result.Promise);
    console.log(result.Pr);
    result.forEach((element) => {
        // console.log(element.code)
       var p1 = document.createElement("pre");
        var p2 = document.createElement("pre");
        var p3 = document.createElement("pre");
        p1.innerHTML = element.status;
        p2.textContent = element.code;
        p3.innerHTML = element.submittedAt;
        document.body.appendChild(p1)
        document.body.appendChild(p2)
        document.body.appendChild(p3)
        // document.createElement("p").innerHTML = element.code;
        // document.createElement("p").innerHTML = element.code;
        
    });
    
});
