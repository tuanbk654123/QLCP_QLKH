import { message } from 'antd';

export const handleApiError = (error, defaultMessage) => {
  console.error('API Error:', error);
  
  if (error.response) {
    const { data, status } = error.response;
    
    // Handle 400 Bad Request / Validation Errors
    if (status === 400) {
        if (data.errors) {
            // ASP.NET Core Validation Errors: { "Field": ["Error1", "Error2"] }
            const errorMessages = Object.values(data.errors).flat();
            if (errorMessages.length > 0) {
                errorMessages.forEach(msg => message.error(msg));
                return;
            }
        }
        if (data.message) {
             message.error(data.message);
             return;
        }
         if (data.title) {
             message.error(data.title);
             return;
        }
    }

    // Handle other errors with message field
    if (data && data.message) {
      message.error(data.message);
      return;
    }
    
    // Fallback to title if available (ProblemDetails)
    if (data && data.title) {
        message.error(data.title);
        return;
    }
  } else if (error.request) {
      // Request was made but no response
      message.error('Không thể kết nối đến máy chủ. Vui lòng kiểm tra kết nối mạng.');
      return;
  }

  // Fallback default message
  message.error(defaultMessage || 'Có lỗi xảy ra. Vui lòng thử lại sau.');
};
